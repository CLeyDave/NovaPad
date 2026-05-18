#pragma warning disable CS0612

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using HidSharp;
using NovaPad.Core.Enums;
using NovaPad.Core.Models;

namespace NovaPad.HID.Services;

public class HidService : IDisposable
{
    public event EventHandler<ControllerInfo>? DeviceArrived;
    public event EventHandler<ControllerInfo>? DeviceRemoved;
    public event EventHandler<ControllerState>? InputReport;

    private readonly ConcurrentDictionary<string, DeviceSession> _sessions = new();
    private CancellationTokenSource? _monitorCts;
    private ManagementEventWatcher? _wmiWatcher;
    private DateTime _lastWmiScan = DateTime.MinValue;
    private DateTime _lastInputScan = DateTime.MinValue;
    private readonly object _wmiLock = new();
    private bool _disposed;

    public IReadOnlyList<ControllerInfo> Devices =>
        _sessions.Values.Select(s => s.Info).ToList().AsReadOnly();

    public int? ReadDeviceBattery(string deviceId)
    {
        if (!_sessions.TryGetValue(deviceId, out var session)) return null;

        var info = session.Info;
        var vid = info.VendorId;
        var pid = info.ProductId;

        try
        {
            if (vid == 0x054C && pid is >= 0x0CE6 and <= 0x0E0B)
                return ReadDualSenseBattery(session);
            if (vid == 0x054C && pid is >= 0x05C4 and <= 0x0BA0)
                return ReadDualShock4Battery(session);
        }
        catch { }

        return null;
    }

    private static int? ReadDualSenseBattery(DeviceSession session)
    {
        var buf = new byte[64];
        buf[0] = 0x09;

        try
        {
            if (session.Stream != null)
            {
                session.Stream.GetFeature(buf);
            }
            else
            {
                using var fallback = session.Device.Open();
                fallback.GetFeature(buf);
            }
        }
        catch { return null; }

        if (buf[0] == 0x09 && buf.Length > 27)
        {
            var val = buf[27];
            var level = (val & 0x0F) * 100 / 10;
            if (level > 0 || (val & 0x0F) != 0)
                return Math.Clamp(level, 0, 100);
        }

        return null;
    }

    private static int? ReadDualShock4Battery(DeviceSession session)
    {
        var buf = new byte[64];
        buf[0] = 0x05;

        try
        {
            if (session.Stream != null)
            {
                session.Stream.GetFeature(buf);
            }
            else
            {
                using var fallback = session.Device.Open();
                fallback.GetFeature(buf);
            }
        }
        catch { return null; }

        if (buf[0] == 0x05 && buf.Length > 30)
        {
            var val = buf[30];
            var level = (val & 0x0F) * 100 / 10;
            return Math.Clamp(level, 0, 100);
        }

        return null;
    }

    public bool SendOutputReport(string deviceId, byte[] report)
    {
        if (!_sessions.TryGetValue(deviceId, out var session)) return false;
        try
        {
            if (session.Stream != null)
            {
                session.Stream.Write(report);
                return true;
            }
            using var fallback = session.Device.Open();
            fallback.Write(report);
            return true;
        }
        catch { return false; }
    }

    public Task StartAsync()
    {
        if (_monitorCts != null) return Task.CompletedTask;
        _monitorCts = new CancellationTokenSource();

        try
        {
            _wmiWatcher = new ManagementEventWatcher(
                "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
            _wmiWatcher.EventArrived += OnWmiDeviceChanged;
            _wmiWatcher.Start();
            Debug.WriteLine("[HidService] WMI device change watcher started.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HidService] WMI watcher failed to start: {ex.Message}");
        }

        _ = SafetyScanAsync(_monitorCts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_wmiWatcher != null)
        {
            try
            {
                _wmiWatcher.Stop();
                _wmiWatcher.EventArrived -= OnWmiDeviceChanged;
                _wmiWatcher.Dispose();
            }
            catch { }
            _wmiWatcher = null;
        }

        _monitorCts?.Cancel();
        _monitorCts = null;
        foreach (var session in _sessions.Values)
            session.Dispose();
        _sessions.Clear();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ControllerInfo>> EnumerateDevicesAsync()
    {
        var infos = new List<ControllerInfo>();
        var devices = DeviceList.Local.GetHidDevices()
            .Where(IsGamepadDevice)
            .ToList();

        foreach (var device in devices)
        {
            var info = CreateControllerInfo(device);
            if (!_sessions.ContainsKey(info.Id))
            {
                var session = new DeviceSession(device, info);
                _sessions[info.Id] = session;
                _ = ReadLoopAsync(session, session.Token);
                OnDeviceArrived(info);
            }
            infos.Add(info);
        }

        return Task.FromResult<IReadOnlyList<ControllerInfo>>(infos);
    }

    public Task<bool> DisconnectDeviceAsync(string deviceId)
    {
        if (_sessions.TryRemove(deviceId, out var session))
        {
            session.Cancel();
            session.Dispose();
            OnDeviceRemoved(session.Info);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private void OnWmiDeviceChanged(object sender, EventArrivedEventArgs e)
    {
        var now = DateTime.UtcNow;
        lock (_wmiLock)
        {
            if ((now - _lastWmiScan).TotalMilliseconds < 200)
                return;
            _lastWmiScan = now;
        }

        try
        {
            DiffScan();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HidService] WMI diff scan failed: {ex.Message}");
        }
    }

    private void DiffScan()
    {
        var current = DeviceList.Local.GetHidDevices()
            .Where(IsGamepadDevice)
            .ToDictionary(d => GetDeviceKey(d), d => d);

        var known = _sessions.Keys.ToHashSet();

        foreach (var key in known)
        {
            if (!current.ContainsKey(key))
            {
                if (_sessions.TryRemove(key, out var session))
                {
                    session.Cancel();
                    session.Dispose();
                    OnDeviceRemoved(session.Info);
                }
            }
        }

        foreach (var kvp in current)
        {
            if (!_sessions.ContainsKey(kvp.Key))
            {
                var device = kvp.Value;
                var info = CreateControllerInfo(device);
                var session = new DeviceSession(device, info);
                _sessions[kvp.Key] = session;
                _ = ReadLoopAsync(session, session.Token);
                OnDeviceArrived(info);
            }
        }
    }

    private async Task SafetyScanAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(300, ct);
                DiffScan();
            }
        }
        catch (TaskCanceledException) { }
    }

    private async Task ReadLoopAsync(DeviceSession session, CancellationToken ct)
    {
        try
        {
            var stream = session.Device.Open();
            session.Stream = stream;

            if (session.Info.Type is ControllerType.DualSense)
            {
                try
                {
                    var outBuf = new byte[64];
                    outBuf[0] = 0x02;
                    outBuf[1] = 0x01;
                    stream.Write(outBuf, 0, outBuf.Length);
                    Debug.WriteLine($"[HidService] Sent output report 0x02 to enable full reports for DualSense");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HidService] Failed to send 0x02 output report: {ex.Message}");
                }
            }

            var bufSize = Math.Min(Math.Max(session.Device.MaxInputReportLength, 64), 512);
            var buf = new byte[bufSize];
            var firstReport = true;

            Debug.WriteLine($"[HidService] Reading from {session.Info.DisplayName} | Path={session.Device.DevicePath} | BufSize={bufSize}");

            while (!ct.IsCancellationRequested && !_disposed)
            {
                var len = await stream.ReadAsync(buf, 0, buf.Length, ct);
                if (len == 0) break;

                if (firstReport)
                {
                    firstReport = false;
                    var hex = Convert.ToHexString(buf, 0, Math.Min(len, 64));
                    Debug.WriteLine($"[HidService] First input report ({len}B): {hex}");
                }

                var state = ParseReport(buf, len, session.Info);
                if (state != null)
                {
                    state.ControllerId = session.Info.Id;
                    state.Timestamp = DateTime.UtcNow;
                    InputReport?.Invoke(this, state);

                    var now = DateTime.UtcNow;
                    if ((now - _lastInputScan).TotalMilliseconds >= 300)
                    {
                        _lastInputScan = now;
                try { DiffScan(); }
                catch (Exception ex) { Debug.WriteLine($"[HidService] DiffScan failed: {ex.Message}"); }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HidService] Read loop ended for {session.Info.DisplayName}: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (_sessions.TryRemove(session.Info.Id, out _))
            {
                session.Dispose();
                OnDeviceRemoved(session.Info);
            }
        }
    }

    private static bool IsGamepadDevice(HidDevice device)
    {
        if (device == null) return false;

        var vid = (ushort)device.VendorID;
        var pid = (ushort)device.ProductID;
        var inpLen = device.MaxInputReportLength;
        var featLen = device.MaxFeatureReportLength;
        var name = GetProductName(device);

        Debug.WriteLine($"[HidService] Candidate: VID={vid:X4} PID={pid:X4} Inp={inpLen} Feat={featLen} Name={name} Path={device.DevicePath}");

        // Filter out mice and keyboards by product name
        if (!string.IsNullOrEmpty(name))
        {
            var lower = name.ToLowerInvariant();
            if (lower.Contains("mouse") || lower.Contains("keyboard") || lower.Contains("keyb"))
                return false;
        }

        // Filter out non-controller HID devices by report size
        // Game controllers typically have 8+ byte input reports
        if (inpLen < 8)
            return false;

        if (!IsKnownController(vid, pid))
            return false;

        // Sony controllers (DS4/DualSense) expose multiple HID interfaces:
        //   gamepad (input 64+), audio (input 8-16), consumer control, vendor.
        // Only the gamepad interface has input >= 64.
        if (vid == 0x054C && inpLen < 64)
            return false;

        // Require feature reports for Sony (gamepad has them, audio/consumer don't)
        if (vid == 0x054C && featLen == 0)
            return false;

        // Reject keyboards and mice by HID usage page
        try
        {
            var descriptor = device.GetReportDescriptor();
            if (descriptor?.DeviceItems?.Count > 0 && descriptor.DeviceItems[0].Usages != null)
            {
                var values = descriptor.DeviceItems[0].Usages.GetAllValues()?.ToList();
                if (values != null && values.Count > 0)
                {
                    var raw = values[0];
                    var usagePage = (ushort)((raw >> 16) & 0xFFFF);
                    var usage = (ushort)(raw & 0xFFFF);
                    if (usagePage == 0x0001 && (usage == 0x0006 || usage == 0x0002))
                        return false;
                }
            }
        }
        catch { }

        Debug.WriteLine($"[HidService] ACCEPTED: VID={vid:X4} PID={pid:X4} Inp={inpLen} Feat={featLen}");
        return true;
    }

    private static bool IsKnownController(ushort vid, ushort pid)
    {
        return (vid, pid) switch
        {
            (0x045E, _) => true,
            (0x054C, _) => true,
            (0x057E, _) => true,
            (0x0079, _) => true,
            (0x2563, _) => true,
            (0x20BC, _) => true,
            (0x2DC8, _) => true,
            (0x1038, _) => true,
            (0x0E8F, _) => true,
            (0x12BD, _) => true,
            (0x1689, _) => true,
            (0x0738, _) => true,
            (0x1BAD, _) => true,
            (0x046D, _) => true,
            (0x28DE, _) => true,
            (0x0E6F, _) => true,
            _ => false
        };
    }

    private static string GetDeviceKey(HidDevice device)
    {
        var serial = GetSerial(device);
        var name = GetProductName(device);
        return $"{device.VendorID:X4}:{device.ProductID:X4}:{serial ?? name ?? device.DevicePath}";
    }

    private static string? GetSerial(HidDevice device)
    {
        try { return device.SerialNumber; }
        catch { return null; }
    }

    private static string? GetProductName(HidDevice device)
    {
        try { return device.ProductName; }
        catch { return null; }
    }

    private static string? GetFriendlyName(HidDevice device)
    {
        try { return device.GetFriendlyName(); }
        catch { return null; }
    }

    private static ControllerInfo CreateControllerInfo(HidDevice device)
    {
        var vid = (ushort)device.VendorID;
        var pid = (ushort)device.ProductID;
        var type = IdentifyControllerType(vid, pid);
        var connection = DetectConnectionType(device, type);
        var maxInput = device.MaxInputReportLength;
        var maxFeature = device.MaxFeatureReportLength;
        Debug.WriteLine($"[HidService] DevicePath: {device.DevicePath}");
        Debug.WriteLine($"[HidService] MaxInputReportLength: {maxInput}, MaxFeatureReportLength: {maxFeature}, Type: {type}, Connection: {connection}");

        return new ControllerInfo
        {
            Id = GetDeviceKey(device),
            DisplayName = GetFriendlyName(device) ?? GetProductName(device) ?? $"Generic Controller ({vid:X4}:{pid:X4})",
            Type = type,
            Connection = connection,
            Manufacturer = GetManufacturerName(vid),
            ProductName = GetProductName(device) ?? $"Controller ({pid:X4})",
            SerialNumber = GetSerial(device) ?? string.Empty,
            VendorId = vid,
            ProductId = pid,
            IsConnected = true,
            HidDevicePath = device.DevicePath,
            MaxInputReportLength = maxInput,
            MaxFeatureReportLength = maxFeature,
            PollingRateHz = 250,
            HasBattery = true,
            HasRumble = type is ControllerType.DualSense or ControllerType.DualShock4,
            HasLed = type is ControllerType.DualSense or ControllerType.DualShock4,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };
    }

    private static ControllerType IdentifyControllerType(ushort vid, ushort pid)
    {
        return (vid, pid) switch
        {
            (0x054C, 0x0CE6) => ControllerType.DualSense,
            (0x054C, 0x0DF2) => ControllerType.DualSense,
            (0x054C, 0x0E0B) => ControllerType.DualSense,
            (0x054C, 0x05C4) => ControllerType.DualShock4,
            (0x054C, 0x09CC) => ControllerType.DualShock4,
            (0x054C, 0x0BA0) => ControllerType.DualShock4,
            (0x045E, 0x028E) => ControllerType.Xbox360,
            (0x045E, 0x028F) => ControllerType.Xbox360,
            (0x045E, 0x02A1) => ControllerType.XboxOne,
            (0x045E, 0x02DD) => ControllerType.XboxOne,
            (0x045E, 0x02E0) => ControllerType.XboxOne,
            (0x045E, 0x02E3) => ControllerType.XboxOne,
            (0x045E, 0x0B12) => ControllerType.XboxSeriesX,
            (0x045E, 0x0B13) => ControllerType.XboxSeriesX,
            (0x045E, 0x0B05) => ControllerType.XboxSeriesX,
            (0x045E, 0x0B22) => ControllerType.XboxSeriesX,
            (0x057E, 0x2009) => ControllerType.NintendoSwitchPro,
            (0x057E, 0x2007) => ControllerType.NintendoJoyConLeft,
            (0x057E, 0x2008) => ControllerType.NintendoJoyConRight,
            (0x28DE, _) => ControllerType.SteamController,
            (0x054C, _) when pid is >= 0x0CE6 and <= 0x0E0B => ControllerType.DualSense,
            (0x054C, _) when pid is >= 0x05C4 and <= 0x0BA0 => ControllerType.DualShock4,
            (0x045E, _) when pid is >= 0x0B00 and <= 0x0B22 => ControllerType.XboxSeriesX,
            (0x045E, _) when pid is >= 0x02A0 and <= 0x02E3 => ControllerType.XboxOne,
            (0x045E, _) when pid is >= 0x0280 and <= 0x028F => ControllerType.Xbox360,
            (0x057E, _) when pid is >= 0x2000 and <= 0x2009 => ControllerType.NintendoSwitchPro,
            _ => ControllerType.GenericHid
        };
    }

    private static ConnectionType DetectConnectionType(HidDevice device, ControllerType type)
    {
        var path = device.DevicePath ?? string.Empty;
        var upper = path.ToUpperInvariant();

        // Bluetooth HID paths on Windows typically contain these markers
        if (upper.Contains("BTH") ||
            upper.Contains("BLUETOOTH") ||
            upper.Contains("BTENUM") ||
            upper.Contains("{00001124-0000-1000-8000-00805F9B34FB}") ||
            upper.Contains("BTHENUM"))
        {
            return ConnectionType.Bluetooth;
        }

        // Xbox Wireless Adapter
        if (upper.Contains("WIRELESS") ||
            upper.Contains("XUSB22") ||
            type == ControllerType.Xbox360 && upper.Contains("&VID_045E"))
        {
            return ConnectionType.WirelessAdapter;
        }

        return ConnectionType.Usb;
    }

    private static string GetManufacturerName(ushort vid) => vid switch
    {
        0x045E => "Microsoft",
        0x054C => "Sony Interactive Entertainment",
        0x057E => "Nintendo",
        0x28DE => "Valve",
        0x046D => "Logitech",
        0x0738 => "Mad Catz",
        0x0E8F => "GreenAsia",
        0x1038 => "SteelSeries",
        0x1689 => "Razer",
        0x20BC => "8BitDo",
        0x2DC8 => "8BitDo",
        0x0079 => "Generic",
        0x2563 => "Generic",
        0x12BD => "Joytech",
         0x1BAD => "Harmonix",
         0x0E6F => "PDP / Gaminja",
        _ => "Generic Manufacturer"
    };

    private static ControllerState? ParseReport(byte[] buf, int len, ControllerInfo info)
    {
        if (len < 4) return null;

        var state = new ControllerState
        {
            ControllerId = info.Id,
            BatteryLevel = -1,
            IsCharging = false,
            LatencyMs = 0
        };

        int offset = buf[0] switch
        {
            0x00 => 0,    // Xbox: no report ID
            0x11 => 2,    // DS4 BT: extra HID timestamp byte after report ID
            _ => 1        // default: skip report ID
        };

        try
        {
            switch (info.Type)
            {
                case ControllerType.DualSense:
                    ParseDualSenseReport(buf, offset, len, state);
                    break;
                case ControllerType.DualShock4:
                    ParseDualShock4Report(buf, offset, len, state);
                    break;
                case ControllerType.Xbox360:
                    ParseXbox360Report(buf, offset, len, state);
                    break;
                case ControllerType.XboxOne:
                case ControllerType.XboxSeriesX:
                    ParseXboxOneReport(buf, offset, len, state);
                    break;
                case ControllerType.NintendoSwitchPro:
                    ParseSwitchProReport(buf, offset, len, state);
                    break;
                default:
                    ParseGenericReport(buf, offset, len, state);
                    break;
            }
        }
        catch { }

        return state;
    }

    private static void ParseDualSenseReport(byte[] buf, int off, int len, ControllerState state)
    {
        if (len < off + 9) return;
        state.LeftStickX = Normalize(buf[off], 0, 255);
        state.LeftStickY = Normalize(buf[off + 1], 0, 255);
        state.RightStickX = Normalize(buf[off + 2], 0, 255);
        state.RightStickY = Normalize(buf[off + 3], 0, 255);
        state.LeftTrigger = buf[off + 4] / 255.0;
        state.RightTrigger = buf[off + 5] / 255.0;
        var btns = (ushort)(buf[off + 6] | (buf[off + 7] << 8));
        SetStandardButtons(btns, state);
        state.DPadUp = (buf[off + 8] & 0x0F) is 7 or 0 or 1;
        state.DPadDown = (buf[off + 8] & 0x0F) is 5 or 4 or 3;
        state.DPadLeft = (buf[off + 8] & 0x0F) is 7 or 6 or 5;
        state.DPadRight = (buf[off + 8] & 0x0F) is 1 or 2 or 3;
        if (len >= off + 79)
        {
            state.BatteryLevel = (buf[off + 78] & 0x0F) * 100 / 10;
            state.IsCharging = (buf[off + 78] & 0x10) != 0;
        }
    }

    private static void ParseDualShock4Report(byte[] buf, int off, int len, ControllerState state)
    {
        if (len < off + 9) return;
        state.LeftStickX = Normalize(buf[off], 0, 255);
        state.LeftStickY = Normalize(buf[off + 1], 0, 255);
        state.RightStickX = Normalize(buf[off + 2], 0, 255);
        state.RightStickY = Normalize(buf[off + 3], 0, 255);
        state.LeftTrigger = buf[off + 4] / 255.0;
        state.RightTrigger = buf[off + 5] / 255.0;

        // DS4: off+6 = Counter(4 bits) + D-Pad(4 bits)
        var dpad = buf[off + 6] & 0x0F;
        state.DPadUp = dpad is 7 or 0 or 1;
        state.DPadDown = dpad is 5 or 4 or 3;
        state.DPadLeft = dpad is 7 or 6 or 5;
        state.DPadRight = dpad is 1 or 2 or 3;

        // DS4: off+7 = Buttons[0]: Square, Cross, Circle, Triangle, L1, R1, L2_d, R2_d
        var b0 = buf[off + 7];
        state.X = (b0 & 0x01) != 0;       // Square → X
        state.A = (b0 & 0x02) != 0;       // Cross → A
        state.B = (b0 & 0x04) != 0;       // Circle → B
        state.Y = (b0 & 0x08) != 0;       // Triangle → Y
        state.LeftBumper = (b0 & 0x10) != 0;
        state.RightBumper = (b0 & 0x20) != 0;

        // DS4: off+8 = Buttons[1]: Share, Options, L3, R3, PS, Touchpad
        var b1 = buf[off + 8];
        state.Back = (b1 & 0x01) != 0;         // Share → Back
        state.Start = (b1 & 0x02) != 0;         // Options → Start
        state.LeftStickClick = (b1 & 0x04) != 0;
        state.RightStickClick = (b1 & 0x08) != 0;
        state.Guide = (b1 & 0x10) != 0;         // PS → Guide

        // USB (0x01): battery at byte 35, BT (0x11): battery at byte 32
        int batteryByte = buf[0] == 0x11 ? 32 : 35;
        if (len >= batteryByte + 1)
        {
            state.BatteryLevel = (buf[batteryByte] & 0x0F) * 100 / 10;
            state.IsCharging = (buf[batteryByte] & 0x10) != 0;
        }
    }

    private static void ParseXbox360Report(byte[] buf, int off, int len, ControllerState state)
    {
        if (len < off + 8) return;
        state.LeftStickX = Normalize(buf[off + 1], 0, 255);
        state.LeftStickY = Normalize(buf[off + 2], 0, 255);
        state.RightStickX = Normalize(buf[off + 3], 0, 255);
        state.RightStickY = Normalize(buf[off + 4], 0, 255);
        state.LeftTrigger = buf[off + 5] / 255.0;
        state.RightTrigger = buf[off + 6] / 255.0;
        var btns = (ushort)(buf[off + 7] | (len > off + 8 ? (buf[off + 8] << 8) : 0));
        state.DPadUp = (btns & 0x0001) != 0;
        state.DPadDown = (btns & 0x0002) != 0;
        state.DPadLeft = (btns & 0x0004) != 0;
        state.DPadRight = (btns & 0x0008) != 0;
        state.Start = (btns & 0x0010) != 0;
        state.Back = (btns & 0x0020) != 0;
        state.LeftStickClick = (btns & 0x0040) != 0;
        state.RightStickClick = (btns & 0x0080) != 0;
        state.A = (btns & 0x1000) != 0;
        state.B = (btns & 0x2000) != 0;
        state.X = (btns & 0x4000) != 0;
        state.Y = (btns & 0x8000) != 0;
        state.Guide = (btns & 0x0400) != 0;
        state.LeftBumper = (btns & 0x0100) != 0;
        state.RightBumper = (btns & 0x0200) != 0;
    }

    private static void ParseXboxOneReport(byte[] buf, int off, int len, ControllerState state)
    {
        if (len < off + 14) return;
        var x = (short)(buf[off + 1] | (buf[off + 2] << 8));
        var y = (short)(buf[off + 3] | (buf[off + 4] << 8));
        var rx = (short)(buf[off + 5] | (buf[off + 6] << 8));
        var ry = (short)(buf[off + 7] | (buf[off + 8] << 8));
        state.LeftStickX = NormalizeShort(x);
        state.LeftStickY = NormalizeShort(y);
        state.RightStickX = NormalizeShort(rx);
        state.RightStickY = NormalizeShort(ry);
        state.LeftTrigger = buf[off + 9] / 255.0;
        state.RightTrigger = buf[off + 10] / 255.0;
        var btns = (ushort)(buf[off + 11] | (buf[off + 12] << 8));
        state.DPadUp = (btns & 0x0001) != 0;
        state.DPadDown = (btns & 0x0002) != 0;
        state.DPadLeft = (btns & 0x0004) != 0;
        state.DPadRight = (btns & 0x0008) != 0;
        state.Start = (btns & 0x0010) != 0;
        state.Back = (btns & 0x0020) != 0;
        state.LeftStickClick = (btns & 0x0040) != 0;
        state.RightStickClick = (btns & 0x0080) != 0;
        state.A = (btns & 0x1000) != 0;
        state.B = (btns & 0x2000) != 0;
        state.X = (btns & 0x4000) != 0;
        state.Y = (btns & 0x8000) != 0;
        state.Guide = (btns & 0x0400) != 0;
        state.LeftBumper = (btns & 0x0100) != 0;
        state.RightBumper = (btns & 0x0200) != 0;
    }

    private static void ParseSwitchProReport(byte[] buf, int off, int len, ControllerState state)
    {
        if (len < off + 10) return;
        state.LeftStickX = NormalizeShort((short)(buf[off + 3] | (buf[off + 4] << 8)));
        state.LeftStickY = NormalizeShort((short)(buf[off + 5] | (buf[off + 6] << 8)));
        state.RightStickX = NormalizeShort((short)(buf[off + 7] | (buf[off + 8] << 8)));
        state.RightStickY = NormalizeShort((short)(buf[off + 9] | (buf[off + 10] << 8)));
        var btns = (ushort)(buf[off + 1] | (buf[off + 2] << 8));
        state.A = (btns & 0x0008) != 0;
        state.B = (btns & 0x0004) != 0;
        state.X = (btns & 0x0002) != 0;
        state.Y = (btns & 0x0001) != 0;
        state.LeftBumper = (btns & 0x0040) != 0;
        state.RightBumper = (btns & 0x0080) != 0;
        state.DPadUp = (btns & 0x0010) != 0;
        state.Start = (btns & 0x0200) != 0;
        state.Back = (btns & 0x0100) != 0;
        state.LeftStickClick = (btns & 0x0008) != 0;
        state.RightStickClick = (btns & 0x0004) != 0;
    }

    private static void ParseGenericReport(byte[] buf, int off, int len, ControllerState state)
    {
        if (len < off + 8)
        {
            if (len > off + 4)
                ParseGenericMinimal(buf, off, state);
            return;
        }

        state.LeftStickX = Normalize(buf[off + 1], 0, 255);
        state.LeftStickY = Normalize(buf[off + 2], 0, 255);
        state.RightStickX = len > off + 3 ? Normalize(buf[off + 3], 0, 255) : 0;
        state.RightStickY = len > off + 4 ? Normalize(buf[off + 4], 0, 255) : 0;
        state.LeftTrigger = len > off + 5 ? buf[off + 5] / 255.0 : 0;
        state.RightTrigger = len > off + 6 ? buf[off + 6] / 255.0 : 0;
        var btns = len > off + 7 ? (int)buf[off + 7] : (len > off + 1 ? (int)buf[off + 1] : 0);
        SetStandardButtons((byte)btns, state);
    }

    private static void ParseGenericMinimal(byte[] buf, int off, ControllerState state)
    {
        state.LeftStickX = Normalize(buf[off], 0, 255);
        state.LeftStickY = Normalize(buf[off + 1], 0, 255);
        if (off + 2 < buf.Length) state.RightStickX = Normalize(buf[off + 2], 0, 255);
        if (off + 3 < buf.Length) state.RightStickY = Normalize(buf[off + 3], 0, 255);
    }

    private static void SetStandardButtons(ushort btns, ControllerState state)
    {
        state.A = (btns & 0x0001) != 0;
        state.B = (btns & 0x0002) != 0;
        state.X = (btns & 0x0004) != 0;
        state.Y = (btns & 0x0008) != 0;
        state.LeftBumper = (btns & 0x0010) != 0;
        state.RightBumper = (btns & 0x0020) != 0;
        state.Back = (btns & 0x0040) != 0;
        state.Start = (btns & 0x0080) != 0;
        state.Guide = (btns & 0x0100) != 0;
        state.LeftStickClick = (btns & 0x0200) != 0;
        state.RightStickClick = (btns & 0x0400) != 0;
    }

    private static void SetStandardButtons(byte btns, ControllerState state)
    {
        state.A = (btns & 0x01) != 0;
        state.B = (btns & 0x02) != 0;
        state.X = (btns & 0x04) != 0;
        state.Y = (btns & 0x08) != 0;
        state.LeftBumper = (btns & 0x10) != 0;
        state.RightBumper = (btns & 0x20) != 0;
        state.Back = (btns & 0x40) != 0;
        state.Start = (btns & 0x80) != 0;
    }

    private static double Normalize(byte val, byte min, byte max)
    {
        var range = max - min;
        if (range == 0) return 0;
        var centered = val - (min + range / 2.0);
        return centered / (range / 2.0);
    }

    private static double NormalizeShort(short val)
    {
        if (val == 0) return 0;
        return val / (val > 0 ? 32767.0 : 32768.0);
    }

    private void OnDeviceArrived(ControllerInfo info) =>
        DeviceArrived?.Invoke(this, info);

    private void OnDeviceRemoved(ControllerInfo info) =>
        DeviceRemoved?.Invoke(this, info);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        foreach (var session in _sessions.Values)
            session.Dispose();
        _sessions.Clear();
    }

    private class DeviceSession : IDisposable
    {
        public HidDevice Device { get; }
        public ControllerInfo Info { get; }
        public HidStream? Stream { get; set; }
        private CancellationTokenSource? _cts;

        public DeviceSession(HidDevice device, ControllerInfo info)
        {
            Device = device;
            Info = info;
            _cts = new CancellationTokenSource();
        }

        public CancellationToken Token => _cts?.Token ?? CancellationToken.None;
        public void Cancel() => _cts?.Cancel();

        public void Dispose()
        {
            Cancel();
            _cts?.Dispose();
            Stream?.Dispose();
            Stream = null;
        }
    }
}
