namespace SideData.TestData;

internal sealed class TestDualDisposable : IDisposable, IAsyncDisposable
{
    public int SyncDisposeCount => _syncDisposeCount;
    public int AsyncDisposeCount => _asyncDisposeCount;
    public bool Disposed => _syncDisposeCount > 0 || _asyncDisposeCount > 0;


    public void Dispose()
        => Interlocked.Increment(ref _syncDisposeCount);


    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _asyncDisposeCount);
        return ValueTask.CompletedTask;
    }


    private int _syncDisposeCount;
    private int _asyncDisposeCount;
}
