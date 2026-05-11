using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class DeviceInfoViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllers;
    private readonly DispatcherTimer _poll;

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

    public bool Idle => !Active;

    public ObservableCollection<ControllerInfo> Pool { get; } = new();

    public DeviceInfoViewModel(IControllerManagerService controllers)
    {
        _controllers = controllers;
        _controllers.ControllerConnected += (_, _) => Sync();
        _controllers.ControllerDisconnected += (_, _) => Sync();
        _controllers.InputReceived += OnInput;

        _poll = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _poll.Tick += (_, _) => Sync();
        _poll.Start();

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

        var elapsed = DateTime.UtcNow - value.FirstSeen;
        Online = $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
    }

    partial void OnActiveChanged(bool value) => OnPropertyChanged(nameof(Idle));

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
        App.Current.Dispatcher.Invoke(() =>
        {
            if (Current is null || s.ControllerId != Current.Id) return;

            var info = _controllers.ConnectedControllers.FirstOrDefault(c => c.Id == s.ControllerId);
            if (info is null) return;

            Response = $"{s.LatencyMs:F1} ms";
            UpdatePower(info);
        });
    }
}
