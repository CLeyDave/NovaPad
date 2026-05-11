using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;
using NovaPad.Core.Events;

namespace NovaPad.Overlay.Services;

public class NamedPipeServer
{
    private const string PipeName = "NovaPadOverlay";
    private const int BufferSize = 65536;
    private readonly EventBus _eventBus;
    private CancellationTokenSource? _cts;

    public NamedPipeServer(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ListenLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token);

                var buffer = new byte[BufferSize];

                while (!token.IsCancellationRequested)
                {
                    var sb = new StringBuilder();
                    int bytesRead;
                    bool gotMessage = false;

                    while ((bytesRead = await server.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        if (server.IsMessageComplete)
                        {
                            gotMessage = true;
                            break;
                        }
                    }

                    if (!gotMessage) break;

                    var json = sb.ToString();
                    if (string.IsNullOrEmpty(json)) continue;

                    var msg = JsonConvert.DeserializeObject<IpcMessage>(json);
                    if (msg == null) continue;

                    DispatchMessage(msg);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(1000, token);
            }
        }
    }

    private void DispatchMessage(IpcMessage msg)
    {
        switch (msg.Type)
        {
            case nameof(DeviceConnectedEvent):
                var connected = msg.Deserialize<DeviceConnectedEvent>();
                if (connected != null) _eventBus.Publish(connected);
                break;

            case nameof(DeviceDisconnectedEvent):
                var disconnected = msg.Deserialize<DeviceDisconnectedEvent>();
                if (disconnected != null) _eventBus.Publish(disconnected);
                break;

            case nameof(BatteryChangedEvent):
                var battery = msg.Deserialize<BatteryChangedEvent>();
                if (battery != null) _eventBus.Publish(battery);
                break;

            case nameof(ProfileChangedEvent):
                var profile = msg.Deserialize<ProfileChangedEvent>();
                if (profile != null) _eventBus.Publish(profile);
                break;

            case nameof(HudUpdateEvent):
                var hud = msg.Deserialize<HudUpdateEvent>();
                if (hud != null) _eventBus.Publish(hud);
                break;

            case nameof(ThemeChangedEvent):
                var theme = msg.Deserialize<ThemeChangedEvent>();
                if (theme != null) _eventBus.Publish(theme);
                break;

            case nameof(WidgetToggleEvent):
                var toggle = msg.Deserialize<WidgetToggleEvent>();
                if (toggle != null) _eventBus.Publish(toggle);
                break;

            case nameof(OverlayConfigEvent):
                var config = msg.Deserialize<OverlayConfigEvent>();
                if (config != null) _eventBus.Publish(config);
                break;
        }
    }
}
