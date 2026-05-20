using System.Collections.Concurrent;
using Windows.Gaming.Input;
using NovaPad.Core.Enums;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using NovaPad.HID.Services;
using Serilog;

namespace NovaPad.WPF.Services;

public class RealControllerManagerService : IControllerManagerService
{
    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<ControllerInfo>? ControllerDisconnected;
    public event EventHandler<ControllerState>? InputReceived;
    public event EventHandler<ControllerInfo>? ControllerUpdated;
    public event EventHandler<(string Id, string Name, int Level)>? LowBattery;

    private readonly HidService _hidService;
    private readonly IControllerNamingService _naming;
    private readonly IBatteryService _battery;
    private readonly IAppSettingsService _appSettings;
    private readonly ConcurrentDictionary<string, ControllerInfo> _controllers = new();
    private readonly ConcurrentDictionary<string, ControllerState> _states = new();
    private readonly ConcurrentDictionary<string, PollingTracker> _polling = new();
    private readonly ConcurrentDictionary<string, BatteryEstimator> _estimators = new();
    private readonly HashSet<string> _notifiedLowBattery = new();
    private readonly PeriodicTimer? _batteryTimer;
    private readonly PeriodicTimer? _inputTimer;
    private readonly Dictionary<string, (Gamepad gamepad, RawGameController raw)> _winGamepads = new();
    private CancellationTokenSource? _batteryCts;
    private CancellationTokenSource? _inputCts;
    private IReadOnlyList<ControllerInfo>? _cachedConnected;
    private bool _cacheDirty = true;

    public IReadOnlyList<ControllerInfo> ConnectedControllers
    {
        get
        {
            if (_cacheDirty || _cachedConnected == null)
            {
                _cachedConnected = _controllers.Values.Where(c => c.IsConnected).ToList().AsReadOnly();
                _cacheDirty = false;
            }
            return _cachedConnected;
        }
    }

    public RealControllerManagerService(IControllerNamingService naming, IBatteryService battery, IAppSettingsService appSettings)
    {
        _naming = naming;
        _battery = battery;
        _appSettings = appSettings;
        _naming.Load();
        _hidService = new HidService();
        _hidService.DeviceArrived += OnDeviceArrived;
        _hidService.DeviceRemoved += OnDeviceRemoved;
        _hidService.InputReport += OnInputReport;

        _batteryCts = new CancellationTokenSource();
        _inputCts = new CancellationTokenSource();
        if (_battery.IsAvailable)
        {
            _batteryTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            _ = PollBatteryAsync(_batteryCts.Token);
        }
        _inputTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(8));
        _ = PollWinGamepadAsync(_inputCts.Token);
    }

    private async Task PollBatteryAsync(CancellationToken ct)
    {
        while (await _batteryTimer!.WaitForNextTickAsync(ct))
        {
            foreach (var ctrl in _controllers.Values.Where(c => c.IsConnected))
            {
                try
                {
                    // Try IBatteryService backends first
                    var result = _battery.GetBatteryLevel(ctrl.VendorId, ctrl.ProductId);
                    if (result != null && result.Level >= 0)
                    {
                    if (result.Level != ctrl.BatteryLevel || ctrl.IsCharging != result.IsCharging)
                        {
                            ApplyEstimatedBattery(ctrl, result.Level, result.IsCharging);
                            UpdateBatteryState(ctrl);
                        }
                        continue;
                    }

                    // Try direct HID feature report as fallback
                    var hidLevel = _hidService.ReadDeviceBattery(ctrl.Id);
                    if (hidLevel.HasValue && hidLevel.Value != ctrl.BatteryLevel)
                    {
                        ApplyEstimatedBattery(ctrl, hidLevel.Value, ctrl.IsCharging);
                        UpdateBatteryState(ctrl);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[RealControllerManagerService] PollBatteryAsync failed for {Id}", ctrl.Id);
                }
            }
        }
    }

    private async Task PollWinGamepadAsync(CancellationToken ct)
    {
        while (await _inputTimer!.WaitForNextTickAsync(ct))
        {
            try
            {
                var gamepadList = Gamepad.Gamepads.ToList();
                var rawList = RawGameController.RawGameControllers.ToList();
                var controllerList = _controllers.Values.Where(c => c.IsConnected).ToList();

                _winGamepads.Clear();
                for (int i = 0; i < Math.Min(gamepadList.Count, controllerList.Count); i++)
                {
                    var raw = i < rawList.Count ? rawList[i] : null;
                    _winGamepads[controllerList[i].Id] = (gamepadList[i], raw!);
                }

                foreach (var (id, (gamepad, raw)) in _winGamepads)
                {
                    var reading = gamepad.GetCurrentReading();

                    var prev = _states.GetValueOrDefault(id);
                    var state = new ControllerState { ControllerId = id };
                    if (prev != null)
                    {
                        state.BatteryLevel = prev.BatteryLevel;
                        state.IsCharging = prev.IsCharging;
                    }
                    state.LeftStickX = reading.LeftThumbstickX;
                    state.LeftStickY = -reading.LeftThumbstickY;
                    state.RightStickX = reading.RightThumbstickX;
                    state.RightStickY = -reading.RightThumbstickY;
                    state.LeftTrigger = reading.LeftTrigger;
                    state.RightTrigger = reading.RightTrigger;
                    state.A = (reading.Buttons & GamepadButtons.A) != 0;
                    state.B = (reading.Buttons & GamepadButtons.B) != 0;
                    state.X = (reading.Buttons & GamepadButtons.X) != 0;
                    state.Y = (reading.Buttons & GamepadButtons.Y) != 0;
                    state.LeftBumper = (reading.Buttons & GamepadButtons.LeftShoulder) != 0;
                    state.RightBumper = (reading.Buttons & GamepadButtons.RightShoulder) != 0;
                    state.Back = (reading.Buttons & GamepadButtons.View) != 0;
                    state.Start = (reading.Buttons & GamepadButtons.Menu) != 0;
                    state.LeftStickClick = (reading.Buttons & GamepadButtons.LeftThumbstick) != 0;
                    state.RightStickClick = (reading.Buttons & GamepadButtons.RightThumbstick) != 0;
                    state.DPadUp = (reading.Buttons & GamepadButtons.DPadUp) != 0;
                    state.DPadDown = (reading.Buttons & GamepadButtons.DPadDown) != 0;
                    state.DPadLeft = (reading.Buttons & GamepadButtons.DPadLeft) != 0;
                    state.DPadRight = (reading.Buttons & GamepadButtons.DPadRight) != 0;

                    state.Guide = raw != null ? LeerGuideDesdeRaw(raw) : false;

                    _states[id] = state;
                    InputReceived?.Invoke(this, state);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[RealControllerManagerService] PollWinGamepadAsync failed");
            }
        }
    }

    private static bool LeerGuideDesdeRaw(RawGameController raw)
    {
        try
        {
            var btns = new bool[raw.ButtonCount];
            var switches = new GameControllerSwitchPosition[raw.SwitchCount];
            var axes = new double[raw.AxisCount];
            raw.GetCurrentReading(btns, switches, axes);

            var guideIdx = -1;
            for (int i = 0; i < raw.ButtonCount; i++)
            {
                var label = (int)raw.GetButtonLabel(i);
                if (label == 4) { guideIdx = i; break; }
            }

            if (guideIdx >= 0) return btns[guideIdx];

            for (int i = raw.ButtonCount - 1; i >= 10; i--)
            {
                if (btns[i])
                {
                    if (btns.Length > 8 && (btns[8] || btns[9]))
                        return false;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateBatteryState(ControllerInfo ctrl)
    {
        if (_states.TryGetValue(ctrl.Id, out var state))
        {
            state.BatteryLevel = ctrl.BatteryLevel;
            state.IsCharging = ctrl.IsCharging;
        }
        ControllerUpdated?.Invoke(this, ctrl);
        CheckLowBattery(ctrl);
    }

    private void CheckLowBattery(ControllerInfo ctrl)
    {
        if (ctrl.BatteryLevel < 0) return;
        var notif = _appSettings.Settings.Notifications;
        if (!notif.ShowBatteryNotifications) return;
        if (ctrl.BatteryLevel > notif.BatteryWarningThreshold)
        {
            _notifiedLowBattery.Remove(ctrl.Id);
            return;
        }
        if (_notifiedLowBattery.Contains(ctrl.Id)) return;
        if (notif.MutedBatteryAlerts.Contains(ctrl.Id)) return;
        _notifiedLowBattery.Add(ctrl.Id);
        LowBattery?.Invoke(this, (ctrl.Id, ctrl.EffectiveName, ctrl.BatteryLevel));
    }

    public Task StartDetectionAsync()
    {
        _ = _hidService.StartAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
                Log.Error(t.Exception, "[RealControllerManagerService] HidService.StartAsync failed");
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }

    public Task StopDetectionAsync()
    {
        _hidService.StopAsync();
        _batteryCts?.Cancel();
        _inputCts?.Cancel();
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ControllerInfo>> ScanForControllersAsync()
    {
        var devices = await _hidService.EnumerateDevicesAsync();
        foreach (var device in devices)
        {
            ApplyCustomName(device);
            TryFillBattery(device);
            _controllers[device.Id] = device;
            _states[device.Id] = new ControllerState
            {
                ControllerId = device.Id,
                BatteryLevel = device.BatteryLevel,
                IsCharging = device.IsCharging
            };
        }
        _cacheDirty = true;
        return ConnectedControllers;
    }

    public ControllerState? GetCurrentState(string controllerId)
    {
        return _states.TryGetValue(controllerId, out var state) ? state : null;
    }

    public Task<bool> SetRumbleAsync(string controllerId, double leftMotor, double rightMotor)
    {
        if (!_controllers.TryGetValue(controllerId, out var ctrl))
            return Task.FromResult(false);

        var left = (byte)Math.Clamp(leftMotor * 255, 0, 255);
        var right = (byte)Math.Clamp(rightMotor * 255, 0, 255);
        var vid = ctrl.VendorId;
        var pid = ctrl.ProductId;

        try
        {
            // DualSense
            if (vid == 0x054C && pid is >= 0x0CE6 and <= 0x0E0B)
            {
                var report = new byte[64];
                report[0] = 0x02;
                report[1] = 0x07;
                report[9] = right;
                report[10] = left;
                report[31] = 0x01;
                report[32] = 0x01;
                return Task.FromResult(_hidService.SendOutputReport(controllerId, report));
            }

            // DualShock 4
            if (vid == 0x054C && pid is >= 0x09CC and <= 0x0BA0)
            {
                if (ctrl.Connection == ConnectionType.Bluetooth)
                {
                    var report = new byte[78];
                    report[0] = 0x11;
                    report[1] = 0x80;
                    report[3] = 0x0F;
                    report[4] = left;
                    report[7] = right;
                    return Task.FromResult(_hidService.SendOutputReport(controllerId, report));
                }

                var usbReport = new byte[32];
                usbReport[0] = 0x05;
                usbReport[1] = 0x07;
                return Task.FromResult(_hidService.SendOutputReport(controllerId, usbReport));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[RealControllerManagerService] SetRumbleAsync failed for {Id}", controllerId);
        }

        return Task.FromResult(false);
    }

    public Task<bool> SetLedColorAsync(string controllerId, byte r, byte g, byte b)
    {
        Log.Warning("[RealControllerManagerService] SetLedColorAsync: not implemented for {Id}", controllerId);
        return Task.FromResult(false);
    }

    public Task<bool> RenameControllerAsync(string controllerId, string newName)
    {
        if (_controllers.TryGetValue(controllerId, out var ctrl))
        {
            ctrl.CustomName = newName;
            _naming.SetCustomName(controllerId, newName);
            ControllerUpdated?.Invoke(this, ctrl);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public void NotifyConnectedControllers()
    {
        foreach (var ctrl in _controllers.Values.Where(c => c.IsConnected))
        {
            ControllerConnected?.Invoke(this, ctrl);
        }
    }

    public async Task<bool> DisconnectControllerAsync(string controllerId)
    {
        if (_controllers.TryGetValue(controllerId, out var ctrl))
        {
            await _hidService.DisconnectDeviceAsync(controllerId);
            ctrl.IsConnected = false;
            _states.TryRemove(controllerId, out _);
            _cacheDirty = true;
            ControllerDisconnected?.Invoke(this, ctrl);
            return true;
        }
        return false;
    }

    private void ApplyCustomName(ControllerInfo info)
    {
        var name = _naming.GetCustomName(info.Id);
        if (!string.IsNullOrEmpty(name))
            info.CustomName = name;
    }

    private void TryFillBattery(ControllerInfo info)
    {
        if (info.BatteryLevel >= 0)
            return;

        try
        {
            var result = _battery.GetBatteryLevel(info.VendorId, info.ProductId);
            if (result != null && result.Level >= 0)
            {
                ApplyEstimatedBattery(info, result.Level, result.IsCharging);
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[RealControllerManagerService] TryFillBattery (backend) failed for {Id}", info.Id);
        }

        try
        {
            var hidLevel = _hidService.ReadDeviceBattery(info.Id);
            if (hidLevel.HasValue)
            {
                ApplyEstimatedBattery(info, hidLevel.Value, info.IsCharging);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[RealControllerManagerService] TryFillBattery (HID) failed for {Id}", info.Id);
        }
    }

    private void OnDeviceArrived(object? sender, ControllerInfo info)
    {
        ApplyCustomName(info);
        TryFillBattery(info);
        _controllers[info.Id] = info;
        _states[info.Id] = new ControllerState
        {
            ControllerId = info.Id,
            BatteryLevel = info.BatteryLevel,
            IsCharging = info.IsCharging
        };
        _cacheDirty = true;
        ControllerConnected?.Invoke(this, info);
    }

    private void OnDeviceRemoved(object? sender, ControllerInfo info)
    {
        if (_controllers.TryGetValue(info.Id, out var ctrl))
        {
            ctrl.IsConnected = false;
            _states.TryRemove(info.Id, out _);
            _estimators.TryRemove(info.Id, out _);
            _cacheDirty = true;
            ControllerDisconnected?.Invoke(this, ctrl);
        }
    }

    private void ApplyEstimatedBattery(ControllerInfo ctrl, int rawPercent, bool isCharging)
    {
        var estimator = _estimators.GetOrAdd(ctrl.Id, _ => new BatteryEstimator());
        ctrl.BatteryLevel = estimator.Update(rawPercent, isCharging, DateTime.UtcNow);
        ctrl.IsCharging = isCharging;
    }

    private void OnInputReport(object? sender, ControllerState state)
    {
        if (_states.TryGetValue(state.ControllerId, out var existing) && state.BatteryLevel >= 0)
        {
            existing.BatteryLevel = state.BatteryLevel;
            existing.IsCharging = state.IsCharging;
        }
        else if (state.BatteryLevel >= 0)
        {
            _states[state.ControllerId] = state;
        }

        if (_controllers.TryGetValue(state.ControllerId, out var ctrl))
        {
            var now = DateTime.UtcNow;

            if (state.BatteryLevel >= 0)
            {
                ApplyEstimatedBattery(ctrl, state.BatteryLevel, state.IsCharging);
            }

            UpdatePollingRate(ctrl, now);
            UpdateSignalStrength(ctrl, now);
            ctrl.LatencyMs = state.LatencyMs > 0 ? state.LatencyMs : EstimateLatency(ctrl.Id, now);
            ctrl.LastSeen = now;
            CheckLowBattery(ctrl);
            ControllerUpdated?.Invoke(this, ctrl);
        }
    }

    private void UpdatePollingRate(ControllerInfo ctrl, DateTime now)
    {
        if (!_polling.TryGetValue(ctrl.Id, out var tracker))
        {
            tracker = new PollingTracker();
            _polling[ctrl.Id] = tracker;
        }

        lock (tracker)
        {
            if (tracker.LastReportTime != default)
            {
                var interval = (now - tracker.LastReportTime).TotalSeconds;
                if (interval > 0 && interval < 1.0)
                {
                    tracker.Intervals.Enqueue(interval);
                    if (tracker.Intervals.Count > 30)
                        tracker.Intervals.Dequeue();

                    var avgInterval = tracker.Intervals.Average();
                    var hz = avgInterval > 0 ? 1.0 / avgInterval : 0;
                    ctrl.PollingRateHz = (int)Math.Round(hz);
                }
            }
            tracker.LastReportTime = now;
        }
    }

    private double EstimateLatency(string controllerId, DateTime now)
    {
        if (!_polling.TryGetValue(controllerId, out var tracker))
            return 0;

        lock (tracker)
        {
            if (tracker.LastReportTime == default) return 0;
            var elapsed = (now - tracker.LastReportTime).TotalMilliseconds;
            return Math.Round(elapsed / 2.0, 1);
        }
    }

    private void UpdateSignalStrength(ControllerInfo ctrl, DateTime now)
    {
        if (ctrl.Connection != ConnectionType.Bluetooth)
        {
            ctrl.SignalStrength = 1.0;
            return;
        }

        if (!_polling.TryGetValue(ctrl.Id, out var tracker))
            return;

        lock (tracker)
        {
            if (tracker.LastReportTime == default)
            {
                ctrl.SignalStrength = 0.5;
                return;
            }

            var elapsed = (now - tracker.LastReportTime).TotalMilliseconds;

            if (elapsed < 16) ctrl.SignalStrength = 1.0;
            else if (elapsed < 20) ctrl.SignalStrength = 0.9;
            else if (elapsed < 30) ctrl.SignalStrength = 0.7;
            else if (elapsed < 50) ctrl.SignalStrength = 0.5;
            else if (elapsed < 100) ctrl.SignalStrength = 0.3;
            else ctrl.SignalStrength = 0.1;
        }
    }

    private class PollingTracker
    {
        public DateTime LastReportTime { get; set; }
        public Queue<double> Intervals { get; } = new(30);
    }
}

