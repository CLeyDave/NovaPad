using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;
using NovaPad.Core.Events;

namespace NovaPad.WPF.Services;

public class OverlayClient : IDisposable
{
    private const string PipeName = "NovaPadOverlay";
    private NamedPipeClientStream? _pipe;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task ConnectAsync(int timeoutMs = 3000)
    {
        _pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        try
        {
            await _pipe.ConnectAsync(timeoutMs);
        }
        catch
        {
            _pipe?.Dispose();
            _pipe = null;
        }
    }

    public async Task SendAsync<T>(T payload)
    {
        await _lock.WaitAsync();
        try
        {
            if (_pipe == null || !_pipe.IsConnected)
            {
                await TryReconnect();
                if (_pipe == null) return;
            }

            var msg = IpcMessage.Create(payload);
            var json = JsonConvert.SerializeObject(msg);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _pipe.WriteAsync(bytes, 0, bytes.Length);
            await _pipe.FlushAsync();
        }
        catch
        {
            _pipe?.Dispose();
            _pipe = null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task TryReconnect()
    {
        try
        {
            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            await _pipe.ConnectAsync(2000);
        }
        catch
        {
            _pipe?.Dispose();
            _pipe = null;
        }
    }

    public void Dispose()
    {
        _lock.Wait();
        try
        {
            _pipe?.Dispose();
            _pipe = null;
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}
