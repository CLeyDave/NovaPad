using NovaPad.Core.Interfaces;

namespace NovaPad.WPF.Services;

public class AggregateBatteryService : IBatteryService
{
    private readonly List<IBatteryService> _backends;

    public bool IsAvailable => _backends.Any(b => b.IsAvailable);

    public AggregateBatteryService(IEnumerable<IBatteryService> backends)
    {
        _backends = backends.ToList();
    }

    public BatteryResult? GetBatteryLevel(ushort vendorId, ushort productId)
    {
        foreach (var backend in _backends)
        {
            try
            {
                var result = backend.GetBatteryLevel(vendorId, productId);
                if (result != null && result.Level >= 0)
                    return result;
            }
            catch
            {
                continue;
            }
        }
        return null;
    }
}
