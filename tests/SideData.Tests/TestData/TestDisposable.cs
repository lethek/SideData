namespace SideData.TestData;

internal sealed class TestDisposable : IDisposable
{
    public int DisposeCount => _disposeCount;
    public bool Disposed => _disposeCount > 0;

    
    public void Dispose()
        => Interlocked.Increment(ref _disposeCount);


    private int _disposeCount;
}