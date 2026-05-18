using System.Collections.Concurrent;
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

    private readonly HidService _hidService;
    private readonly IControllerNamingService _naming;
    private readonly IBatteryService _battery;
    private readonly ConcurrentDictionary<string, ControllerInfo> _controllers = new();
    private readonly ConcurrentDictionary<string, ControllerState> _states = new();
    private readonly ConcurrentDictionary<string, PollingTracker> _polling = new();
    private readonly PeriodicTimer? _batteryTimer;
    private CancellationTokenSource? _batteryCts;
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

    public RealControllerManagerService(IControllerNamingService naming, IBatteryService battery)
    {
        _naming = naming;
        _battery = battery;
        _naming.Load();
        _hidService = new HidService();
        _hidService.DeviceArrived += OnDeviceArrived;
        _hidService.DeviceRemoved += OnDeviceRemoved;
        _hidService.InputReport += OnInputReport;

        if (_battery.IsAvailable)
        {
            _batteryCts = new CancellationTokenSource();
            _batteryTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            _ = PollBatteryAsync(_batteryCts.Token);
        }
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
                        if (result.Level != ctrl.BatteryLevel)
                        {
                            ctrl.BatteryLevel = result.Level;
                            ctrl.IsCharging = result.IsCharging;
                            UpdateBatteryState(ctrl);
                        }
                        continue;
                    }

                    // Try direct HID feature report as fallback
                    var hidLevel = _hidService.ReadDeviceBattery(ctrl.Id);
                    if (hidLevel.HasValue && hidLevel.Value != ctrl.BatteryLevel)
                    {
                        ctrl.BatteryLevel = hidLevel.Value;
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

    private void UpdateBatteryState(ControllerInfo ctrl)
    {
        if (_states.TryGetValue(ctrl.Id, out var state))
        {
            state.BatteryLevel = ctrl.BatteryLevel;
            state.IsCharging = ctrl.IsCharging;
        }
        ControllerUpdated?.Invoke(this, ctrl);
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
        Log.Warning("[RealControllerManagerService] SetRumbleAsync: not implemented for {Id}", controllerId);
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
                info.BatteryLevel = result.Level;
                info.IsCharging = result.IsCharging;
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
                info.BatteryLevel = hidLevel.Value;
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
            _cacheDirty = true;
            ControllerDisconnected?.Invoke(this, ctrl);
        }
    }

    private void OnInputReport(object? sender, ControllerState state)
    {
        _states[state.ControllerId] = state;

        if (_controllers.TryGetValue(state.ControllerId, out var ctrl))
        {
            var now = DateTime.UtcNow;

            if (state.BatteryLevel >= 0)
            {
                ctrl.BatteryLevel = state.BatteryLevel;
                ctrl.IsCharging = state.IsCharging;
            }

            UpdatePollingRate(ctrl, now);
            UpdateSignalStrength(ctrl, now);
            ctrl.LatencyMs = state.LatencyMs > 0 ? state.LatencyMs : EstimateLatency(ctrl.Id, now);
            ctrl.LastSeen = now;
            ControllerUpdated?.Invoke(this, ctrl);
        }

        InputReceived?.Invoke(this, state);
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

