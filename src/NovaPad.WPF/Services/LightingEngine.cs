#pragma warning disable CS0612

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using HidSharp;
using HidSharp.Reports;
using Microsoft.Win32.SafeHandles;
using NovaPad.Core.Enums;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using Serilog;

namespace NovaPad.WPF.Services;

public class LightingEngine : IDisposable
{
    private const int SonyVid = 0x054C;
    private const int DualSenseMinPid = 0x0CE6;
    private const int DualSenseMaxPid = 0x0E0B;
    private const int DualShock4MinPid = 0x05C4;
    private const int DualShock4MaxPid = 0x0BA0;

    private const double TickRate = 30.0;
    private const int MinWriteIntervalMs = 33;
    private const int LerpFramesTotal = 6;
    private const int MaxWriteFails = 3;
    private const double KeepaliveIntervalMs = 5000;

    private readonly IControllerManagerService _controllerManager;
    private readonly ConcurrentDictionary<string, DeviceContext> _devices = new();
    private readonly ConcurrentQueue<LightingCommand> _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _writerTask;

    public LightingEngine(IControllerManagerService controllerManager)
    {
        Log.Warning("[LightingEngine] === STEAM INPUT CHECK ===");
        Log.Warning("[LightingEngine] If RGB does not work, ensure these apps are CLOSED:");
        Log.Warning("[LightingEngine]   - Steam / Steam Big Picture (Steam Input grabs controllers)");
        Log.Warning("[LightingEngine]   - DS4Windows / reWASD / HidHide / DualSenseX");
        Log.Warning("[LightingEngine]   - Any other app that might claim the HID device exclusively");

        _controllerManager = controllerManager;
        _controllerManager.ControllerDisconnected += OnControllerDisconnected;
        _writerTask = Task.Run(() => WriterLoopAsync(_cts.Token));
    }

    public bool IsSupported(string controllerId)
    {
        var ctrl = _controllerManager.ConnectedControllers.FirstOrDefault(c => c.Id == controllerId);
        if (ctrl == null)
        {
            Log.Debug("[LightingEngine] IsSupported({Id}): controller not found in ConnectedControllers", controllerId);
            return false;
        }
        var supported = IsSonyController(ctrl.VendorId, ctrl.ProductId);
        Log.Debug("[LightingEngine] IsSupported({Id}): {Supported} (HasLed={HasLed}, VID={Vid}, PID={Pid})",
            controllerId, supported, ctrl.HasLed, ctrl.VendorId, ctrl.ProductId);
        return supported;
    }

    private static bool IsSonyController(int vid, int pid) =>
        (vid == SonyVid && pid >= DualSenseMinPid && pid <= DualSenseMaxPid) ||
        (vid == SonyVid && pid >= DualShock4MinPid && pid <= DualShock4MaxPid);

    public void Enqueue(LightingCommand cmd)
    {
        _queue.Enqueue(cmd);
        Log.Debug("[LightingEngine] Enqueued {Type} for {Id} (effect={Effect}, priority={P})",
            cmd.Type, cmd.ControllerId, cmd.Effect, cmd.Priority);
    }

    public void StopAll()
    {
        Log.Information("[LightingEngine] StopAll: stopping {Count} active device(s)", _devices.Count);
        foreach (var id in _devices.Keys.ToList())
        {
            _queue.Enqueue(new LightingCommand
            {
                ControllerId = id,
                Type = LightingCommandType.Stop,
                Priority = EffectPriority.Critical
            });
        }
    }

    private async Task WriterLoopAsync(CancellationToken ct)
    {
        var period = TimeSpan.FromMilliseconds(1000.0 / TickRate);

        while (!ct.IsCancellationRequested)
        {
            var tickStart = Stopwatch.GetTimestamp();

            ProcessCommands();
            UpdateControllerInfo();
            TickEffects();
            ApplyLerp();
            await WriteToDevices(ct);

            var elapsed = Stopwatch.GetElapsedTime(tickStart);
            var delay = period - elapsed;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct);
        }
    }

    private void ProcessCommands()
    {
        while (_queue.TryDequeue(out var cmd))
        {
            var ctx = GetOrCreateContext(cmd.ControllerId);
            if (ctx == null)
            {
                Log.Warning("[LightingEngine] Cannot process command: no context for {Id}", cmd.ControllerId);
                continue;
            }

            Log.Debug("[LightingEngine] Processing command: Type={Type}, Controller={Id}, Effect={Effect}, Priority={P}",
                cmd.Type, cmd.ControllerId, cmd.Effect, cmd.Priority);

            switch (cmd.Type)
            {
                case LightingCommandType.SetEffect:
                    if (cmd.Priority >= ctx.Priority)
                    {
                        StartEffect(ctx, cmd);
                    }
                    else
                    {
                        Log.Debug("[LightingEngine] Skipped SetEffect for {Id}: priority {CmdP} < current {CtxP}",
                            cmd.ControllerId, cmd.Priority, ctx.Priority);
                    }
                    break;

                case LightingCommandType.Stop:
                    if (cmd.Priority >= ctx.Priority)
                    {
                        StopEffect(ctx);
                    }
                    else
                    {
                        Log.Debug("[LightingEngine] Skipped Stop for {Id}: priority {CmdP} < current {CtxP}",
                            cmd.ControllerId, cmd.Priority, ctx.Priority);
                    }
                    break;
            }
        }
    }

    private void UpdateControllerInfo()
    {
        foreach (var ctx in _devices.Values)
        {
            ctx.Info = _controllerManager.ConnectedControllers
                .FirstOrDefault(c => c.Id == ctx.ControllerId);
        }
    }

    private void TickEffects()
    {
        foreach (var ctx in _devices.Values)
        {
            if (ctx.ActiveEffect == null) continue;

            switch (ctx.ActiveEffect.Value)
            {
                case LightEffect.Static:
                    break;

                case LightEffect.Pulse:
                {
                    var b = (byte)(Math.Sin(ctx.EffectStep * 0.1) * 127 + 128);
                    ctx.TargetR = (byte)(ctx.BaseR * b / 255);
                    ctx.TargetG = (byte)(ctx.BaseG * b / 255);
                    ctx.TargetB = (byte)(ctx.BaseB * b / 255);
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Rainbow:
                {
                    var (baseH, _, _) = RgbToHsv(ctx.BaseR, ctx.BaseG, ctx.BaseB);
                    var hue = (baseH + ctx.EffectStep * 2) % 360;
                    var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);
                    ctx.TargetR = r; ctx.TargetG = g; ctx.TargetB = b;
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.BatteryLevel:
                {
                    var info = ctx.Info;
                    if (info == null) break;

                    if (info.IsCharging)
                    {
                        var b = (byte)(Math.Sin(ctx.EffectStep * 0.08) * 127 + 128);
                        ctx.TargetR = 0;
                        ctx.TargetG = (byte)(255 * b / 255);
                        ctx.TargetB = 0;
                    }
                    else if (info.BatteryLevel < 0)
                    {
                        ctx.TargetR = 128; ctx.TargetG = 128; ctx.TargetB = 128;
                    }
                    else if (info.BatteryLevel <= 20)
                    {
                        ctx.TargetR = 255; ctx.TargetG = 0; ctx.TargetB = 0;
                    }
                    else if (info.BatteryLevel <= 50)
                    {
                        ctx.TargetR = 255; ctx.TargetG = 165; ctx.TargetB = 0;
                    }
                    else
                    {
                        ctx.TargetR = 0; ctx.TargetG = 255; ctx.TargetB = 0;
                    }
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Strobe:
                {
                    var on = (ctx.EffectStep % 6) < 3;
                    ctx.TargetR = on ? ctx.BaseR : ctx.BaseR2;
                    ctx.TargetG = on ? ctx.BaseG : ctx.BaseG2;
                    ctx.TargetB = on ? ctx.BaseB : ctx.BaseB2;
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Heartbeat:
                {
                    var step = ctx.EffectStep % 40;
                    double v;
                    if (step < 6)
                        v = Math.Sin(step * Math.PI / 6) * 2;
                    else if (step < 12)
                        v = Math.Sin((step - 6) * Math.PI / 6) * 2;
                    else
                        v = 0;
                    v = Math.Clamp(v, 0, 1);
                    ctx.TargetR = (byte)(ctx.BaseR * v);
                    ctx.TargetG = (byte)(ctx.BaseG * v);
                    ctx.TargetB = (byte)(ctx.BaseB * v);
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Fire:
                {
                    var flicker = Random.Shared.Next(40, 256);
                    ctx.TargetR = (byte)(ctx.BaseR * flicker / 255);
                    ctx.TargetG = (byte)(ctx.BaseG * flicker / 300);
                    ctx.TargetB = (byte)(ctx.BaseB * flicker / 300);
                    if (ctx.TargetR < 80) ctx.TargetR = 80;
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Disco:
                {
                    if (ctx.EffectStep % 4 == 0)
                    {
                        var (baseH, _, _) = RgbToHsv(ctx.BaseR, ctx.BaseG, ctx.BaseB);
                        var hue = (baseH + Random.Shared.Next(-40, 41)) % 360;
                        if (hue < 0) hue += 360;
                        var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);
                        ctx.TargetR = r; ctx.TargetG = g; ctx.TargetB = b;
                    }
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Wave:
                {
                    var t = (Math.Sin(ctx.EffectStep * 0.05) + 1.0) / 2.0;
                    ctx.TargetR = (byte)(ctx.BaseR + (ctx.BaseR2 - ctx.BaseR) * t);
                    ctx.TargetG = (byte)(ctx.BaseG + (ctx.BaseG2 - ctx.BaseG) * t);
                    ctx.TargetB = (byte)(ctx.BaseB + (ctx.BaseB2 - ctx.BaseB) * t);
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Meteor:
                {
                    var mStep = ctx.EffectStep % 30;
                    double intensity;
                    if (mStep < 3)
                        intensity = mStep / 3.0;
                    else if (mStep < 6)
                        intensity = 1.0;
                    else if (mStep < 16)
                        intensity = 1.0 - (mStep - 5) / 10.0;
                    else
                        intensity = 0;
                    ctx.TargetR = (byte)(ctx.BaseR * intensity);
                    ctx.TargetG = (byte)(ctx.BaseG * intensity);
                    ctx.TargetB = (byte)(ctx.BaseB * intensity);
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Aurora:
                {
                    var (baseH, baseS, baseV) = RgbToHsv(ctx.BaseR, ctx.BaseG, ctx.BaseB);
                    var hue = (baseH + Math.Sin(ctx.EffectStep * 0.02) * 60) % 360;
                    if (hue < 0) hue += 360;
                    var (r, g, b) = HsvToRgb(hue, baseS * 0.6, Math.Min(1.0, baseV * 1.2));
                    ctx.TargetR = r; ctx.TargetG = g; ctx.TargetB = b;
                    ctx.EffectStep++;
                    break;
                }

                case LightEffect.Storm:
                {
                    if (ctx.EffectStep <= 0)
                    {
                        var isFlash = Random.Shared.Next(4) == 0;
                        ctx.TargetR = isFlash ? ctx.BaseR : (byte)0;
                        ctx.TargetG = isFlash ? ctx.BaseG : (byte)0;
                        ctx.TargetB = isFlash ? ctx.BaseB : (byte)0;
                        ctx.EffectStep = isFlash ? 1 + Random.Shared.Next(2) : 20 + Random.Shared.Next(30);
                    }
                    else
                    {
                        ctx.EffectStep--;
                    }
                    break;
                }
            }

            if (ctx.EffectStep == 0 || ctx.EffectStep % 30 == 0)
            {
                Log.Debug("[LightingEngine] TickEffects: controller={Id}, effect={Effect}, step={Step}, target=RGB({R},{G},{B})",
                    ctx.ControllerId, ctx.ActiveEffect, ctx.EffectStep, ctx.TargetR, ctx.TargetG, ctx.TargetB);
            }
        }
    }

    private void StartEffect(DeviceContext ctx, LightingCommand cmd)
    {
        Log.Information("[LightingEngine] StartEffect: controller={Id}, effect={Effect}, RGB({R},{G},{B}), RGB2({R2},{G2},{B2})",
            ctx.ControllerId, cmd.Effect, cmd.R, cmd.G, cmd.B, cmd.R2, cmd.G2, cmd.B2);

        ctx.StartR = ctx.CurrentR;
        ctx.StartG = ctx.CurrentG;
        ctx.StartB = ctx.CurrentB;
        ctx.LerpFramesRemaining = LerpFramesTotal;

        ctx.ActiveEffect = cmd.Effect;
        ctx.Priority = cmd.Priority;
        ctx.BaseR = cmd.R;
        ctx.BaseG = cmd.G;
        ctx.BaseB = cmd.B;
        ctx.BaseR2 = cmd.R2;
        ctx.BaseG2 = cmd.G2;
        ctx.BaseB2 = cmd.B2;
        ctx.EffectStep = 0;

        if (cmd.Effect == LightEffect.Static)
        {
            ctx.TargetR = cmd.R;
            ctx.TargetG = cmd.G;
            ctx.TargetB = cmd.B;
        }
    }

    private void StopEffect(DeviceContext ctx)
    {
        Log.Information("[LightingEngine] StopEffect: controller={Id}", ctx.ControllerId);

        ctx.StartR = ctx.CurrentR;
        ctx.StartG = ctx.CurrentG;
        ctx.StartB = ctx.CurrentB;
        ctx.TargetR = 0;
        ctx.TargetG = 0;
        ctx.TargetB = 0;
        ctx.LerpFramesRemaining = LerpFramesTotal;

        ctx.ActiveEffect = null;
        ctx.Priority = EffectPriority.User;
        ctx.EffectStep = 0;
    }

    private void ApplyLerp()
    {
        foreach (var ctx in _devices.Values)
        {
            if (ctx.LerpFramesRemaining <= 0)
            {
                ctx.CurrentR = ctx.TargetR;
                ctx.CurrentG = ctx.TargetG;
                ctx.CurrentB = ctx.TargetB;
                continue;
            }

            var done = LerpFramesTotal - ctx.LerpFramesRemaining + 1;
            var t = (float)done / LerpFramesTotal;

            ctx.CurrentR = (byte)(ctx.StartR + (ctx.TargetR - ctx.StartR) * t);
            ctx.CurrentG = (byte)(ctx.StartG + (ctx.TargetG - ctx.StartG) * t);
            ctx.CurrentB = (byte)(ctx.StartB + (ctx.TargetB - ctx.StartB) * t);
            ctx.LerpFramesRemaining--;
        }
    }

    private async Task WriteToDevices(CancellationToken ct)
    {
        foreach (var ctx in _devices.Values)
        {
            if (ct.IsCancellationRequested) break;

            if ((DateTime.UtcNow - ctx.LastWriteTime).TotalMilliseconds < MinWriteIntervalMs)
                continue;

            if (ctx.CurrentR == ctx.WrittenR && ctx.CurrentG == ctx.WrittenG && ctx.CurrentB == ctx.WrittenB)
            {
                if ((DateTime.UtcNow - ctx.LastWriteTime).TotalMilliseconds < KeepaliveIntervalMs)
                    continue;
            }

            if (ctx.Info == null)
            {
                Log.Warning("[LightingEngine] No controller info for {Id}, skipping write.", ctx.ControllerId);
                continue;
            }

            var stream = await GetOrOpenStream(ctx, ct);
            if (stream == null)
            {
                ctx.WriteFailCount++;
                if (ctx.WriteFailCount == 1)
                    Log.Warning("[LightingEngine] Cannot open HID stream for {Id} (fail #{F})",
                        ctx.ControllerId, ctx.WriteFailCount);
                if (ctx.WriteFailCount >= MaxWriteFails)
                {
                    Log.Error("[LightingEngine] Max write fails ({F}) for {Id}, cleaning up stream.",
                        ctx.WriteFailCount, ctx.ControllerId);
                    CleanupStream(ctx);
                }
                continue;
            }

            try
            {
                await ctx.StreamLock.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                continue;
            }

            try
            {
                Log.Debug("[LightingEngine] Writing RGB({R},{G},{B}) to {Id}",
                    ctx.CurrentR, ctx.CurrentG, ctx.CurrentB, ctx.ControllerId);
                WriteColorToStream(ctx, stream);
                ctx.WrittenR = ctx.CurrentR;
                ctx.WrittenG = ctx.CurrentG;
                ctx.WrittenB = ctx.CurrentB;
                ctx.LastWriteTime = DateTime.UtcNow;
                ctx.WriteFailCount = 0;
                Log.Debug("[LightingEngine] Write succeeded for {Id} (RGB({R},{G},{B}))",
                    ctx.ControllerId, ctx.WrittenR, ctx.WrittenG, ctx.WrittenB);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LightingEngine] HID write failed for {Id}", ctx.ControllerId);
                ctx.WriteFailCount++;
                if (ctx.WriteFailCount >= MaxWriteFails)
                {
                    try { ctx.Stream?.Dispose(); } catch { }
                    ctx.Stream = null;
                }
            }
            finally
            {
                ctx.StreamLock.Release();
            }
        }
    }

    private async Task<HidStream?> GetOrOpenStream(DeviceContext ctx, CancellationToken ct)
    {
        if (ctx.Stream != null && ctx.Stream.CanWrite)
            return ctx.Stream;

        await ctx.StreamLock.WaitAsync(ct);
        try
        {
            if (ctx.Stream != null && ctx.Stream.CanWrite)
                return ctx.Stream;

            try { ctx.Stream?.Dispose(); } catch { }
            ctx.Stream = null;

            if (ctx.Info == null)
            {
                Log.Warning("[LightingEngine] GetOrOpenStream: no controller info for {Id}", ctx.ControllerId);
                return null;
            }

            var vid = ctx.Info.VendorId;
            var pid = ctx.Info.ProductId;
            var hidPath = ctx.Info.HidDevicePath;

            // Enumerate ALL HID devices for this VID with descriptor info
            var allForVid = DeviceList.Local.GetHidDevices(vid)
                .Where(d => (ushort)d.ProductID == pid)
                .ToList();

            Log.Debug("[LightingEngine] GetOrOpenStream: found {Count} total HID device(s) (VID={Vid}, PID={Pid}) for {Id}",
                allForVid.Count, vid, pid, ctx.ControllerId);

            foreach (var d in allForVid)
            {
                var (usagePage, usage) = GetDeviceUsage(d);
                Log.Debug("[LightingEngine]   HID: UP=0x{UsagePage:X4} U=0x{Usage:X4} " +
                          "InLen={InLen} OutLen={OutLen} FeatLen={FeatLen} " +
                          "Path={Path}",
                    usagePage, usage,
                    d.MaxInputReportLength, d.MaxOutputReportLength, d.MaxFeatureReportLength,
                    d.DevicePath);
            }

            // Select device: prefer a match by HID device path (for identical controllers),
            // then fall back to the one with the largest Output/Feature/Input report
            HidDevice? device;
            if (!string.IsNullOrEmpty(hidPath))
            {
                device = allForVid.FirstOrDefault(d =>
                    d.DevicePath.Equals(hidPath, StringComparison.OrdinalIgnoreCase));
                if (device != null)
                {
                    Log.Debug("[LightingEngine] GetOrOpenStream: exact device path match for {Id}", ctx.ControllerId);
                }
            }
            else
            {
                device = null;
            }

            device ??= allForVid
                .OrderByDescending(d => d.MaxOutputReportLength)
                .ThenByDescending(d => d.MaxFeatureReportLength)
                .ThenByDescending(d => d.MaxInputReportLength)
                .FirstOrDefault();

            if (device == null)
            {
                Log.Warning("[LightingEngine] GetOrOpenStream: no HID device found with VID={Vid}, PID={Pid} for {Id}",
                    vid, pid, ctx.ControllerId);
                return null;
            }

            var (selUsagePage, selUsage) = GetDeviceUsage(device);
            Log.Debug("[LightingEngine] GetOrOpenStream: selected device " +
                      "UP=0x{UsagePage:X4} U=0x{Usage:X4} " +
                      "OutLen={OutLen} for {Id}",
                selUsagePage, selUsage, device.MaxOutputReportLength, ctx.ControllerId);

            if (device.TryOpen(out var stream))
            {
                Log.Information("[LightingEngine] HID stream opened for {Name} ({Id}) at {Path}",
                    ctx.Info.EffectiveName, ctx.ControllerId, device.DevicePath);
                ctx.Stream = stream;
                ctx.WriteFailCount = 0;
                return stream;
            }

            Log.Warning("[LightingEngine] TryOpen failed for {Id} at {Path}",
                ctx.ControllerId, device.DevicePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LightingEngine] Error opening HID stream for {Id}", ctx.ControllerId);
            ctx.Stream = null;
        }
        finally
        {
            ctx.StreamLock.Release();
        }
        return null;
    }

    private void WriteColorToStream(DeviceContext ctx, HidStream stream)
    {
        var ctrl = ctx.Info;
        if (ctrl == null) return;

        if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualSenseMinPid && ctrl.ProductId <= DualSenseMaxPid)
        {
            var report = new byte[64];
            report[0] = 0x02;
            report[1] = 0x04;       // Enable flags: bit 2 = lightbar color only (not 0xFF to avoid invalid bits)
            report[2] = 0x00;
            report[9] = 0x00;       // Right rumble
            report[10] = 0x00;      // Left rumble
            report[21] = 0x00;      // Player LED mask
            report[22] = 0x00;      // Player LED brightness
            report[31] = 0x01;      // Lightbar enable
            report[32] = 0x01;      // Lightbar extended enable
            report[41] = ctx.CurrentR;  // Lightbar R (newer firmware)
            report[42] = ctx.CurrentG;  // Lightbar G
            report[43] = ctx.CurrentB;  // Lightbar B
            report[45] = ctx.CurrentR;  // Lightbar R (older firmware fallback)
            report[46] = ctx.CurrentG;  // Lightbar G
            report[47] = ctx.CurrentB;  // Lightbar B
            Log.Debug("[LightingEngine] WriteColorToStream: DualSense USB format, RGB({R},{G},{B}), report[0..48]={Bytes}",
                ctx.CurrentR, ctx.CurrentG, ctx.CurrentB, BitConverter.ToString(report, 0, 48));

            stream.Write(report);
            Log.Debug("[LightingEngine] WriteColorToStream: DualSense HidSharp done for {Id}", ctx.ControllerId);
        }
        else if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualShock4MinPid && ctrl.ProductId <= DualShock4MaxPid)
        {
            if (ctrl.Connection == ConnectionType.Bluetooth)
            {
                // --- BT init handshake (feature report 0x02) â€” one-time ---
                if (!ctx.IsBtInitialized)
                {
                    if (InitializeDs4BtMode(stream))
                        ctx.IsBtInitialized = true;
                }

                var report = new byte[78];
                report[0] = 0x11;
                report[1] = 0x80;
                report[2] = 0x00;
                report[3] = 0x0F;       // Flags: rumble + color + flash + 0x08
                report[4] = 0x00;
                report[5] = 0x00;
                report[6] = 0x00;
                report[7] = 0x00;       // Rumble right
                report[8] = ctx.CurrentR;
                report[9] = ctx.CurrentG;
                report[10] = ctx.CurrentB;
                report[11] = 0x00;      // Flash on
                report[12] = 0x00;      // Flash off

                var crc32 = CalculateCrc32WithHeader(report, 0, 74);
                report[74] = (byte)(crc32 & 0xFF);
                report[75] = (byte)((crc32 >> 8) & 0xFF);
                report[76] = (byte)((crc32 >> 16) & 0xFF);
                report[77] = (byte)((crc32 >> 24) & 0xFF);

                Log.Debug("[LightingEngine] WriteColorToStream: DS4 BT RGB({R},{G},{B}), CRC32=0x{Crc:X8}, report[0..14]={Bytes}",
                    ctx.CurrentR, ctx.CurrentG, ctx.CurrentB, crc32, BitConverter.ToString(report, 0, 14));

                stream.Write(report);
                Log.Debug("[LightingEngine] WriteColorToStream: DS4 BT stream.Write done for {Id}", ctx.ControllerId);
            }
            else
            {
                var report = new byte[32];
                report[0] = 0x05;
                report[1] = 0x07;
                report[2] = 0x00;
                report[3] = 0x00;
                report[4] = 0x00;
                report[5] = 0x00;
                report[6] = ctx.CurrentR;
                report[7] = ctx.CurrentG;
                report[8] = ctx.CurrentB;
                report[9] = 0xFF;
                report[10] = 0x00;
                Log.Debug("[LightingEngine] WriteColorToStream: DS4 USB format, RGB({R},{G},{B}), report[0..12]={Bytes}",
                    ctx.CurrentR, ctx.CurrentG, ctx.CurrentB, BitConverter.ToString(report, 0, 12));

                stream.Write(report);
                Log.Debug("[LightingEngine] WriteColorToStream: HidSharp WriteFile done for {Id}", ctx.ControllerId);
            }
        }
    }

    private static uint CalculateCrc32WithHeader(byte[] reportDataWithoutHeader, int offset, int length)
    {
        uint crc = 0xFFFFFFFF;
        byte hidpHeader = 0xA2;

        crc ^= hidpHeader;
        for (int j = 0; j < 8; j++)
        {
            if ((crc & 1) != 0)
                crc = (crc >> 1) ^ 0xEDB88320;
            else
                crc >>= 1;
        }

        for (int i = offset; i < offset + length; i++)
        {
            crc ^= reportDataWithoutHeader[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
        }
        return ~crc;
    }

    private static (ushort UsagePage, ushort Usage) GetDeviceUsage(HidDevice device)
    {
        try
        {
            var descriptor = device.GetReportDescriptor();
            if (descriptor?.DeviceItems == null || descriptor.DeviceItems.Count == 0)
                return (0, 0);

            var topItem = descriptor.DeviceItems[0];
            if (topItem.Usages == null)
                return (0, 0);

            var values = topItem.Usages.GetAllValues()?.ToList();
            if (values == null || values.Count == 0)
                return (0, 0);

            var raw = values[0];
            return ((ushort)((raw >> 16) & 0xFFFF), (ushort)(raw & 0xFFFF));
        }
        catch
        {
            return (0, 0);
        }
    }

    private DeviceContext? GetOrCreateContext(string controllerId)
    {
        if (_devices.TryGetValue(controllerId, out var existing))
        {
            Log.Debug("[LightingEngine] GetOrCreateContext: retrieved existing context for {Id}", controllerId);
            return existing;
        }

        var ctrl = _controllerManager.ConnectedControllers.FirstOrDefault(c => c.Id == controllerId);
        if (ctrl == null)
        {
            Log.Warning("[LightingEngine] GetOrCreateContext: controller not found in ConnectedControllers: {Id}", controllerId);
            return null;
        }

        if (!ctrl.HasLed && !IsSonyController(ctrl.VendorId, ctrl.ProductId))
        {
            Log.Warning("[LightingEngine] GetOrCreateContext: controller has no LED support: {Id} (VID={Vid}, PID={Pid})",
                controllerId, ctrl.VendorId, ctrl.ProductId);
            return null;
        }

        var ctx = new DeviceContext { ControllerId = controllerId, Info = ctrl };
        _devices[controllerId] = ctx;
        Log.Information("[LightingEngine] Created device context for {Name} ({Id}) â€” VID={Vid} PID={Pid}",
            ctrl.EffectiveName, controllerId, ctrl.VendorId, ctrl.ProductId);
        return ctx;
    }

    private void CleanupStream(DeviceContext ctx)
    {
        try { ctx.Stream?.Dispose(); } catch { }
        ctx.Stream = null;
        ctx.WriteFailCount = 0;
    }

    private void OnControllerDisconnected(object? sender, ControllerInfo e)
    {
        Log.Information("[LightingEngine] Controller disconnected: {Name} ({Id})", e.EffectiveName, e.Id);
        if (_devices.TryRemove(e.Id, out var ctx))
        {
            ctx.StreamLock.Wait();
            try
            {
                try { ctx.Stream?.Dispose(); } catch { }
            }
            finally
            {
                ctx.StreamLock.Release();
            }
        }
    }

    private static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double h = 0, s = 0, v = max;
        double delta = max - min;

        if (delta > 0)
        {
            s = delta / max;
            if (Math.Abs(max - rd) < 0.001)
                h = 60 * (((gd - bd) / delta) % 6);
            else if (Math.Abs(max - gd) < 0.001)
                h = 60 * (((bd - rd) / delta) + 2);
            else
                h = 60 * (((rd - gd) / delta) + 4);
        }

        if (h < 0) h += 360;
        return (h, s, v);
    }

    private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        var hi = (int)(h / 60) % 6;
        var f = h / 60 - hi;
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);

        return hi switch
        {
            0 => ((byte)(v * 255), (byte)(t * 255), (byte)(p * 255)),
            1 => ((byte)(q * 255), (byte)(v * 255), (byte)(p * 255)),
            2 => ((byte)(p * 255), (byte)(v * 255), (byte)(t * 255)),
            3 => ((byte)(p * 255), (byte)(q * 255), (byte)(v * 255)),
            4 => ((byte)(t * 255), (byte)(p * 255), (byte)(v * 255)),
            _ => ((byte)(v * 255), (byte)(p * 255), (byte)(q * 255)),
        };
    }

    public void Dispose()
    {
        Log.Information("[LightingEngine] Disposing...");
        _controllerManager.ControllerDisconnected -= OnControllerDisconnected;
        _cts.Cancel();
        try { _writerTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts.Dispose();

        foreach (var ctx in _devices.Values)
        {
            try { ctx.Stream?.Dispose(); } catch { }
            try { ctx.StreamLock.Dispose(); } catch { }
        }
        _devices.Clear();
    }

    private byte[]? ReadFeatureReport(HidStream stream, byte reportId, int bufferSize = 64)
    {
        try
        {
            var buf = new byte[bufferSize];
            buf[0] = reportId;
            stream.GetFeature(buf);
            Log.Debug("[LightingEngine] GetFeature(0x{ReportId:X2}): {Bytes}",
                reportId, BitConverter.ToString(buf, 0, Math.Min(buf.Length, 48)));
            return buf;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[LightingEngine] GetFeature(0x{ReportId:X2}) failed", reportId);
            return null;
        }
    }

    private bool InitializeDs4BtMode(HidStream stream)
    {
        var buf = ReadFeatureReport(stream, 0x02, 64);
        if (buf == null) return false;

        // buf[0] is the mode/status byte from the DS4 (0x00 = uninitialized, 0x02 = BT mode)
        if (buf[0] != 0)
        {
            Log.Debug("[LightingEngine] DS4 BT already initialized (mode=0x{Mode:X2})", buf[0]);
            return true;
        }

        Log.Warning("[LightingEngine] DS4 BT feature report 0x02 first byte is 0x00 â€” sending BT mode init");

        var initBuf = new byte[64];
        initBuf[0] = 0x02;
        initBuf[1] = 0x02;

        try
        {
            stream.SetFeature(initBuf);
            Log.Information("[LightingEngine] SET_FEATURE(0x02, mode=0x02) sent for BT init");

            Thread.Sleep(30);
            var verifyBuf = ReadFeatureReport(stream, 0x02, 64);
            if (verifyBuf != null && verifyBuf[0] != 0)
            {
                Log.Information("[LightingEngine] DS4 BT init verified: feature report 0x02 mode=0x{Mode:X2}",
                    verifyBuf[0]);
                return true;
            }

            Log.Warning("[LightingEngine] DS4 BT init sent but verification failed: first byte still 0x{Byte:X2}",
                verifyBuf?[0] ?? 0);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LightingEngine] SET_FEATURE(0x02) for BT init failed");
            return false;
        }
    }

    private static class NativeMethods
    {
        public enum EFileAccess : uint
        {
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
        }

        public enum EFileShare : uint
        {
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002,
        }

        public enum ECreationDisposition : uint
        {
            OPEN_EXISTING = 3,
        }

        public enum EFileAttributes : uint
        {
            NORMAL = 0x80,
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            EFileAccess dwDesiredAccess,
            EFileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            ECreationDisposition dwCreationDisposition,
            EFileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(
            SafeFileHandle hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool HidD_SetOutputReport(
            SafeFileHandle HidDeviceObject,
            byte[] ReportBuffer,
            int ReportBufferLength);
    }

    private class DeviceContext
    {
        public string ControllerId = string.Empty;
        public ControllerInfo? Info;

        public HidStream? Stream;
        public readonly SemaphoreSlim StreamLock = new(1, 1);

        public byte WrittenR, WrittenG, WrittenB;
        public byte CurrentR, CurrentG, CurrentB;
        public byte TargetR, TargetG, TargetB;
        public byte StartR, StartG, StartB;
        public int LerpFramesRemaining;

        public LightEffect? ActiveEffect;
        public EffectPriority Priority;
        public int EffectStep;
        public byte BaseR, BaseG, BaseB;
        public byte BaseR2, BaseG2, BaseB2;

        public DateTime LastWriteTime = DateTime.MinValue;
        public int WriteFailCount;
        public bool IsBtInitialized;
    }
}


