using SuperSocket.Server;

namespace PyroCache;

public sealed class PyroSession : AppSession
{
    public long ClientId { get; private set; }
    
    public string? ClientName { get; set; }

    public static readonly SemaphoreSlim _lock = new(1);

    public static bool IsPaused { get; private set; }
        
    private static long _counter;

    protected override ValueTask OnSessionConnectedAsync()
    {
        Interlocked.Increment(ref _counter);
        ClientId = _counter;

        return base.OnSessionConnectedAsync();
    }

    public async Task Pause(int milliseconds)
    {
        var tcs = new TaskCompletionSource();
        
        await _lock.WaitAsync();
        IsPaused = true;
        Task.Run(() => Task
            .Delay(TimeSpan.FromMilliseconds(milliseconds))
            .ContinueWith(_ =>
            {
                IsPaused = false;
                return _lock.Release();
            }));
    }
    
    public void Unpause() => IsPaused = false;
}