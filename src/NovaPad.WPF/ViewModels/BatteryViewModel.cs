using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class BatteryViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;

    public ObservableCollection<BatteryItem> Controllers { get; } = new();

    [ObservableProperty]
    private bool _hasControllers;

    public BatteryViewModel(IControllerManagerService controllerManager)
    {
        _controllerManager = controllerManager;
        _controllerManager.ControllerConnected += OnControllersChanged;
        _controllerManager.ControllerDisconnected += OnControllersChanged;
        _controllerManager.ControllerUpdated += OnControllersChanged;
        _controllerManager.InputReceived += OnInputReceived;
        Refresh();
    }

    private void OnControllersChanged(object? sender, ControllerInfo e) => DispatchRefresh();
    private void OnInputReceived(object? sender, ControllerState state) => DispatchRefresh();

    private void DispatchRefresh()
    {
        App.Current?.Dispatcher.Invoke(Refresh);
    }

    private void Refresh()
    {
        var visible = _controllerManager.ConnectedControllers
            .Where(c => !c.IsEmulated
                && !string.IsNullOrEmpty(c.EffectiveName)
                && c.EffectiveName != "Unknown Controller"
                && c.Type != Core.Enums.ControllerType.Unknown)
            .ToList();

        HasControllers = visible.Count > 0;
        var currentIds = visible.Select(c => c.Id).ToHashSet();
        var existingIds = Controllers.Select(c => c.Id).ToHashSet();

        foreach (var removedId in existingIds.Except(currentIds).ToList())
        {
            var item = Controllers.FirstOrDefault(c => c.Id == removedId);
            if (item != null) Controllers.Remove(item);
        }

        foreach (var ctrl in visible.Where(c => !existingIds.Contains(c.Id)))
        {
            Controllers.Add(new BatteryItem
            {
                Id = ctrl.Id,
                Name = ctrl.EffectiveName,
                Level = ctrl.BatteryLevel,
                IsCharging = ctrl.IsCharging
            });
        }

        foreach (var ctrl in visible)
        {
            var item = Controllers.FirstOrDefault(c => c.Id == ctrl.Id);
            if (item != null)
            {
                item.Level = ctrl.BatteryLevel;
                item.IsCharging = ctrl.IsCharging;
            }
        }
    }
}

public partial class BatteryItem : ObservableObject
{
    private const double MaxFillHeight = 160.0;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    private int _level = -1;
    public int Level
    {
        get => _level;
        set
        {
            if (SetProperty(ref _level, value))
            {
                OnPropertyChanged(nameof(PercentText));
                OnPropertyChanged(nameof(FillHeight));
                OnPropertyChanged(nameof(ChargingText));
                OnPropertyChanged(nameof(FillColor));
                OnPropertyChanged(nameof(BarColor));
            }
        }
    }

    private bool _isCharging;
    public bool IsCharging
    {
        get => _isCharging;
        set
        {
            if (SetProperty(ref _isCharging, value))
            {
                OnPropertyChanged(nameof(ChargingText));
                OnPropertyChanged(nameof(FillColor));
                OnPropertyChanged(nameof(BarColor));
            }
        }
    }

    public string PercentText => Level >= 0 ? $"{Level}%" : "--%";
    public string ChargingText => IsCharging ? "Cargando" : "";
    public double FillHeight => Level >= 0 ? Level / 100.0 * MaxFillHeight : 0.0;

    public string FillColor
    {
        get
        {
            if (Level < 0) return "#666666";
            if (IsCharging) return "#4FC3F7";
            if (Level <= 20) return "#EF5350";
            if (Level <= 50) return "#FFB300";
            return "#66BB6A";
        }
    }

    private SolidColorBrush? _barColor;
    private Color _lastColor;
    public SolidColorBrush BarColor
    {
        get
        {
            var color = ColorConverter.ConvertFromString(FillColor) as Color? ?? Colors.Gray;
            if (_barColor == null || _lastColor != color)
            {
                _barColor = new SolidColorBrush(color);
                _lastColor = color;
            }
            return _barColor;
        }
    }
}
