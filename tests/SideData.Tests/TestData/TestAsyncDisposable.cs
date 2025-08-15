namespace SideData.TestData;

internal sealed class TestAsyncDisposable : IAsyncDisposable
{
    public int DisposeCount => _disposeCount;
    public bool Disposed => _disposeCount > 0;

    
    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposeCount);
        return ValueTask.CompletedTask;
    }

    
    private int _disposeCount;
}