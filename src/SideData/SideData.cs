using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;


namespace SideData;

public readonly struct SideData<TK> where TK : class
{
    internal SideData(TK obj)
        => _hostObject = obj;


    public void Set<T>(T value)
        => AttachedData<T>.Table.AddOrUpdate(_hostObject, new(value));


    public void Add<T>(T value)
        => AttachedData<T>.Table.Add(_hostObject, new(value));


    public bool TryAdd<T>(T value)
        => AttachedData<T>.Table.TryAdd(_hostObject, new(value));


    public void Remove<T>()
        => AttachedData<T>.Table.Remove(_hostObject);


    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public T Get<T>()
        => AttachedData<T>.Table.TryGetValue(_hostObject, out var box)
            ? box.Value
            : throw new KeyNotFoundException();


    public T GetOrDefault<T>(T defaultValue = default!)
        => TryGet<T>(out var value)
            ? value
            : defaultValue;


    public bool TryGet<T>([MaybeNullWhen(false)] out T value)
    {
        if (AttachedData<T>.Table.TryGetValue(_hostObject, out var box)) {
            value = box.Value;
            return true;
        } else {
            value = default(T);
            return false;
        }
    }


    private readonly TK _hostObject;


    private sealed record RecordBox<T>(T Value)
    {
        public readonly T Value = Value;
    }


    private static class AttachedData<T>
    {
        internal static readonly ConditionalWeakTable<TK, RecordBox<T>> Table = new();
    }
}
