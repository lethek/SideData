using System.Runtime.CompilerServices;

namespace SideData.Internals;

// Reference equality comparer for netstandard2.0 scenarios
internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static readonly ReferenceEqualityComparer<T> Instance = new();

    
    public bool Equals(T? x, T? y)
        => ReferenceEquals(x, y);


    public int GetHashCode(T obj)
        => RuntimeHelpers.GetHashCode(obj);

    
    private ReferenceEqualityComparer() { }
}
