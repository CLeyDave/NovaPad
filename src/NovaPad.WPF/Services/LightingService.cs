using System.Collections.Concurrent;
using HidSharp;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.Services;

public class LightingService : ILightingService, IDisposable
{
    private const int SonyVid = 0x054C;
    private const int DualSenseMinPid = 0x0CE6;
    private const int DualSenseMaxPid = 0x0E0B;
    private const int DualShock4MinPid = 0x05C4;
    private const int DualShock4MaxPid = 0x0BA0;

    private readonly ConcurrentDictionary<string, HidStream> _streams = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _effects = new();
    private readonly IControllerManagerService _controllerManager;

    public LightingService(IControllerManagerService controllerManager)
    {
        _controllerManager = controllerManager;
    }

    public bool IsSupported(string controllerId)
    {
        var ctrl = _controllerManager.ConnectedControllers
            .FirstOrDefault(c => c.Id == controllerId);
        if (ctrl == null) return false;

        return (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualSenseMinPid && ctrl.ProductId <= DualSenseMaxPid)
            || (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualShock4MinPid && ctrl.ProductId <= DualShock4MaxPid);
    }

    private HidStream? GetOrOpenStream(string controllerId)
    {
        if (_streams.TryGetValue(controllerId, out var existing) && existing.CanWrite)
            return existing;

        var device = DeviceList.Local.GetHidDevices(SonyVid)
            .FirstOrDefault(d => d.GetSerialNumber() == controllerId
                || d.DevicePath.Contains(controllerId));
        if (device == null) return null;

        if (device.TryOpen(out var stream))
        {
            _streams[controllerId] = stream;
            return stream;
        }
        return null;
    }

    public void SetColor(string controllerId, byte r, byte g, byte b)
    {
        Stop(controllerId);

        var stream = GetOrOpenStream(controllerId);
        if (stream == null) return;

        var ctrl = _controllerManager.ConnectedControllers.FirstOrDefault(c => c.Id == controllerId);
        if (ctrl == null) return;

        if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualSenseMinPid && ctrl.ProductId <= DualSenseMaxPid)
            WriteDualSenseColor(stream, r, g, b);
        else if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualShock4MinPid && ctrl.ProductId <= DualShock4MaxPid)
            WriteDualShock4Color(stream, r, g, b);
    }

    public void SetEffect(string controllerId, LightEffect effect, byte r, byte g, byte b)
    {
        Stop(controllerId);

        var cts = new CancellationTokenSource();
        _effects[controllerId] = cts;
        var token = cts.Token;

        var ctrl = _controllerManager.ConnectedControllers.FirstOrDefault(c => c.Id == controllerId);
        if (ctrl == null) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var stream = GetOrOpenStream(controllerId);
                if (stream == null) return;

                switch (effect)
                {
                    case LightEffect.Static:
                        WriteColor(ctrl, stream, r, g, b);
                        break;

                    case LightEffect.Pulse:
                        await RunPulse(ctrl, stream, r, g, b, token);
                        break;

                    case LightEffect.Rainbow:
                        await RunRainbow(ctrl, stream, token);
                        break;

                    case LightEffect.BatteryLevel:
                        await RunBatteryLevel(ctrl, stream, token);
                        break;
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }, token);
    }

    public void Stop(string controllerId)
    {
        if (_effects.TryRemove(controllerId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void StopAll()
    {
        foreach (var id in _effects.Keys.ToList())
            Stop(id);
    }

    private void WriteColor(ControllerInfo ctrl, HidStream stream, byte r, byte g, byte b)
    {
        if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualSenseMinPid && ctrl.ProductId <= DualSenseMaxPid)
            WriteDualSenseColor(stream, r, g, b);
        else if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualShock4MinPid && ctrl.ProductId <= DualShock4MaxPid)
            WriteDualShock4Color(stream, r, g, b);
    }

    private static void WriteDualSenseColor(HidStream stream, byte r, byte g, byte b)
    {
        var report = new byte[48];
        report[0] = 0x02;
        report[1] = 0xFF;
        report[45] = r;
        report[46] = g;
        report[47] = b;
        stream.Write(report);
    }

    private static void WriteDualShock4Color(HidStream stream, byte r, byte g, byte b)
    {
        var report = new byte[32];
        report[0] = 0x05;
        report[1] = 0xFF;
        report[6] = r;
        report[7] = g;
        report[8] = b;
        stream.Write(report);
    }

    private static async Task RunPulse(ControllerInfo ctrl, HidStream stream, byte r, byte g, byte b, CancellationToken token)
    {
        var step = 0;
        while (!token.IsCancellationRequested)
        {
            var brightness = (byte)(Math.Sin(step * 0.1) * 127 + 128);
            var pr = (byte)(r * brightness / 255);
            var pg = (byte)(g * brightness / 255);
            var pb = (byte)(b * brightness / 255);

            if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualSenseMinPid && ctrl.ProductId <= DualSenseMaxPid)
                WriteDualSenseColor(stream, pr, pg, pb);
            else
                WriteDualShock4Color(stream, pr, pg, pb);

            step++;
            await Task.Delay(30, token);
        }
    }

    private static async Task RunRainbow(ControllerInfo ctrl, HidStream stream, CancellationToken token)
    {
        var step = 0;
        while (!token.IsCancellationRequested)
        {
            var (r, g, b) = HsvToRgb((step * 2) % 360, 1.0, 1.0);

            if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualSenseMinPid && ctrl.ProductId <= DualSenseMaxPid)
                WriteDualSenseColor(stream, r, g, b);
            else
                WriteDualShock4Color(stream, r, g, b);

            step++;
            await Task.Delay(30, token);
        }
    }

    private async Task RunBatteryLevel(ControllerInfo ctrl, HidStream stream, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var level = ctrl.BatteryLevel;
            byte r, g, b;

            if (level < 0) { r = 128; g = 128; b = 128; }
            else if (level <= 20) { r = 255; g = 0; b = 0; }
            else if (level <= 50) { r = 255; g = 165; b = 0; }
            else { r = 0; g = 255; b = 0; }

            if (ctrl.VendorId == SonyVid && ctrl.ProductId >= DualSenseMinPid && ctrl.ProductId <= DualSenseMaxPid)
                WriteDualSenseColor(stream, r, g, b);
            else
                WriteDualShock4Color(stream, r, g, b);

            await Task.Delay(1000, token);
        }
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
        StopAll();
        foreach (var stream in _streams.Values)
        {
            try { stream.Dispose(); } catch { }
        }
        _streams.Clear();
    }
}
