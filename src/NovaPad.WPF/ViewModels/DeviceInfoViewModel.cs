using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class DeviceInfoViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllers;
    private readonly DispatcherTimer _poll;
    private CancellationTokenSource? _rumbleCts;

    [ObservableProperty]
    private ControllerInfo? _current;

    [ObservableProperty]
    private string _name = "--";

    [ObservableProperty]
    private string _kind = "--";

    [ObservableProperty]
    private string _link = "--";

    [ObservableProperty]
    private string _vendor = "--";

    [ObservableProperty]
    private string _device = "--";

    [ObservableProperty]
    private string _hid = "--";

    [ObservableProperty]
    private string _features = "--";

    [ObservableProperty]
    private string _version = "--";

    [ObservableProperty]
    private string _power = "--";

    [ObservableProperty]
    private double _charge;

    [ObservableProperty]
    private bool _charging;

    [ObservableProperty]
    private string _chargeLabel = "";

    [ObservableProperty]
    private string _response = "--";

    [ObservableProperty]
    private string _rate = "-- Hz";

    [ObservableProperty]
    private string _strength = "--";

    [ObservableProperty]
    private string _online = "--";

    [ObservableProperty]
    private bool _active;

    [ObservableProperty]
    private double _rumbleLeft = 0.5;

    [ObservableProperty]
    private double _rumbleRight = 0.5;

    [ObservableProperty]
    private bool _rumbleTesting;

    public bool Idle => !Active;
    public bool RumbleSupported => Current?.HasRumble == true;
    public bool RumbleVisible => Active && RumbleSupported;

    public ObservableCollection<ControllerInfo> Pool { get; } = new();

    public DeviceInfoViewModel(IControllerManagerService controllers)
    {
        _controllers = controllers;
        _controllers.ControllerConnected += OnControllerEvent;
        _controllers.ControllerDisconnected += OnControllerEvent;
        _controllers.InputReceived += OnInput;

        _poll = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _poll.Tick += OnPollTick;
        _poll.Start();

        Sync();
    }

    private void OnControllerEvent(object? sender, ControllerInfo e) => DispatchSync();
    private void OnPollTick(object? sender, EventArgs e) => DispatchSync();

    private void DispatchSync()
    {
        if (Application.Current?.Dispatcher.CheckAccess() == false)
            Application.Current.Dispatcher.Invoke(Sync);
        else
            Sync();
    }

    partial void OnCurrentChanged(ControllerInfo? value)
    {
        if (value is null)
        {
            Active = false;
            Name = "--"; Kind = "--"; Link = "--";
            Vendor = "--"; Device = "--"; Hid = "--";
            Features = "--"; Version = "--";
            Power = "--"; ChargeLabel = "";
            Response = "--"; Rate = "-- Hz"; Strength = "--"; Online = "--";
            return;
        }

        Active = true;
        Name = value.EffectiveName;
        Kind = $"{value.Type} · {value.Connection}";
        Link = value.Connection.ToString();
        Vendor = $"{value.VendorId:X4}";
        Device = $"{value.ProductId:X4}";
        Hid = value.HidDevicePath ?? "--";
        Version = value.FirmwareVersion;

        var caps = new List<string>();
        if (value.HasBattery) caps.Add("Battery");
        if (value.HasTouchpad) caps.Add("Touchpad");
        if (value.HasGyroscope) caps.Add("Gyro");
        if (value.HasAccelerometer) caps.Add("Accel");
        if (value.HasRumble) caps.Add("Rumble");
        if (value.HasLed) caps.Add("LED");
        Features = caps.Count > 0 ? string.Join(", ", caps) : "--";

        UpdatePower(value);
        Response = $"{value.LatencyMs:F1} ms";
        Rate = $"{value.PollingRateHz} Hz";
        Strength = $"{value.SignalStrength * 100:F0}%";

        OnPropertyChanged(nameof(RumbleSupported));
        OnPropertyChanged(nameof(RumbleVisible));

        var elapsed = DateTime.UtcNow - value.FirstSeen;
        Online = $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
    }

    partial void OnActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(Idle));
        OnPropertyChanged(nameof(RumbleSupported));
        OnPropertyChanged(nameof(RumbleVisible));
    }

    [RelayCommand]
    private async Task TestRumbleAsync()
    {
        if (Current == null || !Current.HasRumble) return;
        RumbleTesting = true;
        _rumbleCts?.Cancel();
        _rumbleCts = new CancellationTokenSource();
        var token = _rumbleCts.Token;
        try
        {
            await _controllers.SetRumbleAsync(Current.Id, RumbleLeft, RumbleRight);
            await Task.Delay(1500, token);
            await _controllers.SetRumbleAsync(Current.Id, 0, 0);
        }
        catch (TaskCanceledException) { }
        finally
        {
            await _controllers.SetRumbleAsync(Current?.Id ?? "", 0, 0);
            RumbleTesting = false;
        }
    }

    [RelayCommand]
    private async Task StopRumbleAsync()
    {
        _rumbleCts?.Cancel();
        if (Current != null)
            await _controllers.SetRumbleAsync(Current.Id, 0, 0);
        RumbleTesting = false;
    }

    private void UpdatePower(ControllerInfo c)
    {
        if (c.BatteryLevel < 0) { Power = "--"; return; }
        Power = $"{c.BatteryLevel}%";
        Charge = c.BatteryLevel / 100.0;
        Charging = c.IsCharging;
        ChargeLabel = c.IsCharging ? "charging" : "";
    }

    private void Sync()
    {
        var visible = _controllers.ConnectedControllers
            .Where(c => !c.IsEmulated
                && !string.IsNullOrEmpty(c.EffectiveName)
                && c.EffectiveName != "Unknown Controller"
                && c.Type != Core.Enums.ControllerType.Unknown)
            .ToList();

        var prev = Current?.Id;
        Pool.Clear();
        foreach (var c in visible) Pool.Add(c);

        Current = !string.IsNullOrEmpty(prev)
            ? Pool.FirstOrDefault(c => c.Id == prev)
            : Pool.FirstOrDefault();
    }

    private void OnInput(object? sender, ControllerState s)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            if (Current is null || s.ControllerId != Current.Id) return;

            var info = _controllers.ConnectedControllers.FirstOrDefault(c => c.Id == s.ControllerId);
            if (info is null) return;

            Response = $"{s.LatencyMs:F1} ms";
            UpdatePower(info);
        });
    }
}
