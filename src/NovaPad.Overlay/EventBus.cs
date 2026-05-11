namespace NovaPad.Overlay;

public class EventBus
{
    private readonly Dictionary<Type, Delegate> _handlers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        lock (_handlers)
        {
            if (_handlers.TryGetValue(typeof(T), out var existing))
                _handlers[typeof(T)] = Delegate.Combine(existing, handler);
            else
                _handlers[typeof(T)] = handler;
        }
    }

    public void Publish<T>(T eventData)
    {
        Delegate? d;
        lock (_handlers)
        {
            _handlers.TryGetValue(typeof(T), out d);
        }
        (d as Action<T>)?.Invoke(eventData);
    }
}
