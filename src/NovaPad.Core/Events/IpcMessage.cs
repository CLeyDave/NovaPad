using Newtonsoft.Json;

namespace NovaPad.Core.Events;

public class IpcMessage
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;

    public static IpcMessage Create<T>(T payload)
    {
        return new IpcMessage
        {
            Type = typeof(T).Name,
            Payload = JsonConvert.SerializeObject(payload)
        };
    }

    public T? Deserialize<T>()
    {
        return JsonConvert.DeserializeObject<T>(Payload);
    }
}
