using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;


namespace SideData;

public readonly struct TypedSideData<TK> where TK : class
{
    internal TypedSideData(TK obj)
        => _hostObject = obj ?? throw new ArgumentNullException(nameof(obj));


    /// <summary>
    /// Associates the specified value with the host object, replacing any existing value of the same type.
    /// </summary>
    /// <typeparam name="T">The type of the value to associate.</typeparam>
    /// <param name="value">The value to set for the host object.</param>
    public void Set<T>(T value)
    {
        var box = AttachedData<T>.Table.GetValue(_hostObject, _ => new Box<T>());
        box.Value = value;
    }


    /// <summary>
    /// Adds the specified value to the collection of data attached to the host object.
    /// If an entry of the same type already exists, it will be overwritten.
    /// </summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="value">The value to attach to the host object.</param>
    public void Add<T>(T value)
        => AttachedData<T>.Table.Add(_hostObject, new(value));


    /// <summary>
    /// Attempts to add the specified value as side‑data associated with the host object.
    /// </summary>
    /// <typeparam name="T">The type of the data being added.</typeparam>
    /// <param name="value">The instance to attach to the host object.</param>
    /// <returns>
    /// <c>true</c> if the value was successfully added; otherwise, <c>false</c>, indicating that an entry of the same type already exists.
    /// </returns>
    public bool TryAdd<T>(T value)
#if NET7_0_OR_GREATER
        => AttachedData<T>.Table.TryAdd(_hostObject, new(value));
#else
    {
        lock (AttachedData<T>.Table) {
            if (AttachedData<T>.Table.TryGetValue(_hostObject, out _)) {
                return false;
            }

            AttachedData<T>.Table.Add(_hostObject, new Box<T>(value));
            return true;
        }
    }
#endif


    /// <summary>
    /// Removes the side‑data object of type <typeparamref name="T"/> that was previously attached to the host object.
    /// </summary>
    /// <typeparam name="T">The type of the data being removed.</typeparam>
    public void Remove<T>()
        => AttachedData<T>.Table.Remove(_hostObject);


    /// <summary>
    /// Retrieves the side‑data object of type <typeparamref name="T"/> that was previously attached
    /// to the host object.
    /// </summary>
    /// <typeparam name="T">The type of the data being retrieved.</typeparam>
    /// <returns>The instance of <typeparamref name="T"/> associated with the host object.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no value of type <typeparamref name="T"/> has been attached to the host
    /// object.
    /// </exception>
    public T Get<T>()
        => AttachedData<T>.Table.TryGetValue(_hostObject, out var box) && box.Value is { } v
            ? v
            : throw new KeyNotFoundException();


    /// <summary>
    /// Retrieves the value of the specified type from the host object if it exists; otherwise returns the supplied default.
    /// </summary>
    /// <typeparam name="T">The type of the data to retrieve.</typeparam>
    /// <param name="defaultValue">The value returned when no data of type <typeparamref name="T"/> is attached to the host object.</param>
    /// <returns>The stored value if present; otherwise <paramref name="defaultValue"/>.</returns>
    public T GetOrDefault<T>(T defaultValue = default!)
        => TryGet<T>(out var value)
            ? value
            : defaultValue;


    /// <summary>
    /// Retrieves the value of type <typeparamref name="T"/> attached to the host object.
    /// If no such value exists, creates a new one using the provided factory delegate,
    /// attaches it to the host, and then returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve or create.</typeparam>
    /// <param name="newValue">
    /// A delegate that receives the host object of type <typeparamref name="TK"/>
    /// and produces a new instance of <typeparamref name="T"/> when no existing
    /// value is found.
    /// </param>
    /// <returns>
    /// The existing value of type <typeparamref name="T"/> if one was already attached;
    /// otherwise, the newly created value returned by <paramref name="newValue"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="newValue"/> is null.
    /// </exception>
    public T GetOrAdd<T>(Func<TK, T> newValue)
    {
        if (newValue is null) {
            throw new ArgumentNullException(nameof(newValue));
        }

        return AttachedData<T>.Table
            .GetValue(_hostObject, tk => new(newValue(tk)))
            .Value!;
    }


    /// <summary>
    /// Attempts to retrieve the value of type <typeparamref name="T"/> attached to the host object.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve.</typeparam>
    /// <param name="value">
    /// When this method returns, contains the retrieved value if found; otherwise the default value for the type.
    /// </param>
    /// <returns><c>true</c> if a value of type <typeparamref name="T"/> was successfully retrieved; otherwise <c>false</c>.</returns>
    public bool TryGet<T>(
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out T value)
    {
        if (AttachedData<T>.Table.TryGetValue(_hostObject, out var box) && box.Value is { } v) {
            value = v;
            return true;
        }

        value = default!;
        return false;
    }


    private readonly TK _hostObject;


    private sealed class Box<T>(T value = default!)
    {
        public T Value = value;
    }


    private static class AttachedData<T>
    {
        internal static readonly ConditionalWeakTable<TK, Box<T>> Table = new();
    }
}
