using System;

namespace NovaPad.WPF.Services;

public class BatteryEstimator
{
    private double _displayPercent = -1;
    private int _lastHardwarePercent = -1;
    private int _transitionTargetPercent = -1;
    private double _transitionStartPercent;
    private DateTime _transitionStartTime;

    public int Update(int hardwarePercent, bool isCharging, DateTime now)
    {
        if (_displayPercent < 0 || _lastHardwarePercent < 0)
        {
            _displayPercent = hardwarePercent;
            _lastHardwarePercent = hardwarePercent;
            _transitionTargetPercent = hardwarePercent;
            return (int)Math.Round(_displayPercent);
        }

        if (isCharging || hardwarePercent > _lastHardwarePercent)
        {
            _displayPercent = hardwarePercent;
            _lastHardwarePercent = hardwarePercent;
            _transitionTargetPercent = hardwarePercent;
            return (int)Math.Round(_displayPercent);
        }

        if (hardwarePercent != _lastHardwarePercent)
        {
            _transitionStartPercent = _displayPercent;
            _transitionTargetPercent = hardwarePercent;
            _transitionStartTime = now;
            _lastHardwarePercent = hardwarePercent;
        }

        if (_displayPercent != _transitionTargetPercent)
        {
            var elapsed = (now - _transitionStartTime).TotalSeconds;
            var progress = Math.Min(elapsed / 20.0, 1.0);
            _displayPercent = _transitionStartPercent + (_transitionTargetPercent - _transitionStartPercent) * progress;
        }

        return (int)Math.Round(_displayPercent);
    }

    public void Reset()
    {
        _displayPercent = -1;
        _lastHardwarePercent = -1;
    }
}
