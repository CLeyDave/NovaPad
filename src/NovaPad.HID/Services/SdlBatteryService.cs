using NovaPad.Core.Interfaces;
using SDL2;

namespace NovaPad.HID.Services;

public class SdlBatteryService : IBatteryService, IDisposable
{
    private bool _initialized;
    private bool _available;

    public bool IsAvailable => _available;

    public SdlBatteryService()
    {
        try
        {
            var result = SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK);
            if (result == 0)
            {
                _initialized = true;
                _available = true;
            }
        }
        catch
        {
            _available = false;
        }
    }

    public BatteryResult? GetBatteryLevel(ushort vendorId, ushort productId)
    {
        if (!_initialized)
            return null;

        try
        {
            int numJoysticks = SDL.SDL_NumJoysticks();
            for (int i = 0; i < numJoysticks; i++)
            {
                var vid = (ushort)SDL.SDL_JoystickGetDeviceVendor(i);
                var pid = (ushort)SDL.SDL_JoystickGetDeviceProduct(i);

                if (vid != vendorId || pid != productId)
                    continue;

                IntPtr joystick = SDL.SDL_JoystickOpen(i);
                if (joystick == IntPtr.Zero)
                    continue;

                try
                {
                    var power = SDL.SDL_JoystickCurrentPowerLevel(joystick);
                    return MapPowerLevel(power);
                }
                finally
                {
                    SDL.SDL_JoystickClose(joystick);
                }
            }
        }
        catch
        {
            _available = false;
        }

        return null;
    }

    private static BatteryResult? MapPowerLevel(SDL.SDL_JoystickPowerLevel power)
    {
        var result = new BatteryResult { Source = BatterySource.Sdl2 };

        switch (power)
        {
            case SDL.SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_EMPTY:
                result.Level = 5;
                break;
            case SDL.SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_LOW:
                result.Level = 25;
                break;
            case SDL.SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_MEDIUM:
                result.Level = 50;
                break;
            case SDL.SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_FULL:
                result.Level = 90;
                break;
            case SDL.SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_WIRED:
                result.Level = 100;
                break;
            case SDL.SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_MAX:
                result.Level = 100;
                break;
            default:
                return null;
        }

        return result;
    }

    public void Dispose()
    {
        if (_initialized)
        {
            try { SDL.SDL_Quit(); }
            catch { }
            _initialized = false;
        }
    }
}
