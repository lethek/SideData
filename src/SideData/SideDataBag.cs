using System.Dynamic;
using SideData.Internals;

namespace SideData;

public sealed class SideDataBag(bool disposeOnReplace = false) : DynamicObject, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether replaced values are automatically disposed when overwritten.
    /// </summary>
    public bool DisposeOnReplace { get; } = disposeOnReplace;


    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        lock (_gate) {
            ThrowIfDisposed();
            return _members.TryGetValue(binder.Name, out result!);
        }
    }


    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        lock (_gate) {
            ThrowIfDisposed();

            if (_members.TryGetValue(binder.Name, out var old)) {
                Untrack(old, dispose: DisposeOnReplace);
            }

            _members[binder.Name] = value;
            Track(value);
            return true;
        }
    }


    /// <summary>
    /// Attempts to delete a member from the bag.
    /// When a member is deleted, it uses the bag's <see cref="DisposeOnReplace"/> setting to determine whether to dispose the removed value.
    /// </summary>
    public override bool TryDeleteMember(DeleteMemberBinder binder)
    {
        lock (_gate) {
            ThrowIfDisposed();

            if (_members.TryGetValue(binder.Name, out var old)) {
                _members.Remove(binder.Name);
                Untrack(old, dispose: DisposeOnReplace);
                return true;
            }

            return false;
        }
    }


    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        lock (_gate) {
            ThrowIfDisposed();

            // Support a single string key only
            if (indexes is null || indexes.Length != 1 || indexes[0] is not string key) {
                result = null;
                return false;
            }

            return _members.TryGetValue(key, out result!);
        }
    }


    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
    {
        lock (_gate) {
            ThrowIfDisposed();

            // Support a single string key only
            if (indexes is null || indexes.Length != 1 || indexes[0] is not string key) {
                return false;
            }

            if (_members.TryGetValue(key, out var old)) {
                Untrack(old, dispose: DisposeOnReplace);
            }

            _members[key] = value;
            Track(value);
            return true;
        }
    }


    public override IEnumerable<string> GetDynamicMemberNames()
    {
        lock (_gate) {
            ThrowIfDisposed();
            return _members.Keys.ToArray();
        }
    }


    /// <summary>
    /// Gets or sets a value in the dynamic bag with the specified member name.
    /// When getting, returns the value associated with the provided name, or null if no such member exists.
    /// When setting, updates the member value, optionally disposing of the previous value if <see cref="DisposeOnReplace"/> is enabled.
    /// </summary>
    /// <param name="name">The name of the dynamic member.</param>
    /// <returns>The value associated with the specified name.</returns>
    public object? this[string name] {
        get {
            lock (_gate) {
                ThrowIfDisposed();
                return _members.TryGetValue(name, out var v) ? v : null;
            }
        }
        set {
            lock (_gate) {
                ThrowIfDisposed();
                if (_members.TryGetValue(name, out var old)) {
                    Untrack(old, dispose: DisposeOnReplace);
                }

                _members[name] = value;
                Track(value);
            }
        }
    }


    /// <summary>
    /// Tries to get a typed value stored under the specified name.
    /// </summary>
    /// <typeparam name="T">The expected type of the stored value.</typeparam>
    /// <param name="name">The member name.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with <paramref name="name"/> if the member exists and is
    /// assignable to <typeparamref name="T"/>; otherwise, the default value for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// True if a member exists with the given name and is compatible with <typeparamref name="T"/> (including null for
    /// reference and nullable types); otherwise, false.
    /// </returns>
    public bool TryGet<T>(string name, out T? value)
    {
        lock (_gate) {
            ThrowIfDisposed();

            if (_members.TryGetValue(name, out var v)) {
                if (v is T t) {
                    value = t;
                    return true;
                }

                // If stored value is null and T can accept null (ref or Nullable<T>) treat as success
                if (v is null && default(T) is null) {
                    value = default;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }


    /// <summary>
    /// Gets the typed value stored under the specified name, or returns a default value when missing or incompatible.
    /// </summary>
    /// <typeparam name="T">The expected type of the stored value.</typeparam>
    /// <param name="name">The member name.</param>
    /// <param name="defaultValue">The value to return when the member is missing or not compatible with <typeparamref name="T"/>.</param>
    /// <returns>
    /// The stored value if present and assignable to <typeparamref name="T"/>; otherwise <paramref name="defaultValue"/>.
    /// When the stored value is null and <typeparamref name="T"/> can accept null (ref or Nullable<T>), returns default(T).
    /// </returns>
    public T? GetOrDefault<T>(string name, T? defaultValue = default)
    {
        lock (_gate) {
            ThrowIfDisposed();

            if (_members.TryGetValue(name, out var v)) {
                if (v is T t) {
                    return t;
                }

                if (v is null && default(T) is null) {
                    return default;
                }
            }

            return defaultValue;
        }
    }


    /// <summary>
    /// Gets the value stored under the specified name, or creates and stores one using <paramref name="factory"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the stored value.</typeparam>
    /// <param name="name">The member name.</param>
    /// <param name="factory">A delegate that produces the value to store when no member exists under <paramref name="name"/>.</param>
    /// <returns>The existing value if present, otherwise the newly created value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="factory"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The bag has been disposed.</exception>
    /// <exception cref="InvalidCastException">A member already exists under <paramref name="name"/> but is not assignable to <typeparamref name="T"/>.</exception>
    /// <remarks>
    /// The factory runs outside the internal lock so that user code cannot deadlock or reenter the bag. As a result,
    /// under concurrent calls the factory may run more than once; only one produced value wins and is stored, while any
    /// losing value is discarded (and disposed if it implements <see cref="IDisposable"/>/<see cref="IAsyncDisposable"/>).
    /// For exactly-once initialization, store a <see cref="Lazy{T}"/>: <c>bag.GetOrAdd(name, () =&gt; new Lazy&lt;T&gt;(create))</c>.
    /// </remarks>
    public T GetOrAdd<T>(string name, Func<T> factory)
    {
        if (name is null) {
            throw new ArgumentNullException(nameof(name));
        }
        if (factory is null) {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (_gate) {
            ThrowIfDisposed();
            if (_members.TryGetValue(name, out var existing)) {
                return CastOrThrow<T>(existing, name);
            }
        }

        var created = factory();

        object? raced;
        bool lostRace = false;
        bool wasDisposed = false;

        lock (_gate) {
            if (_disposed) {
                wasDisposed = true;
                raced = null;
            } else if (_members.TryGetValue(name, out raced)) {
                lostRace = true;
            } else {
                _members[name] = created;
                Track(created);
                return created;
            }
        }

        DisposeOrphan(created);

        if (wasDisposed) {
            throw new ObjectDisposedException(nameof(SideDataBag));
        }

        return lostRace
            ? CastOrThrow<T>(raced, name)
            : created;
    }


    /// <summary>
    /// Gets the value stored under the specified name, or stores and returns <paramref name="value"/> when no member exists.
    /// </summary>
    /// <typeparam name="T">The expected type of the stored value.</typeparam>
    /// <param name="name">The member name.</param>
    /// <param name="value">The value to store when no member exists under <paramref name="name"/>.</param>
    /// <returns>The existing value if present, otherwise <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The bag has been disposed.</exception>
    /// <exception cref="InvalidCastException">A member already exists under <paramref name="name"/> but is not assignable to <typeparamref name="T"/>.</exception>
    /// <remarks>
    /// When an existing member wins, <paramref name="value"/> is returned to the caller untouched and is never disposed by the
    /// bag, mirroring <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/> semantics. Only a value
    /// actually stored becomes owned and tracked for disposal.
    /// </remarks>
    public T GetOrAdd<T>(string name, T value)
    {
        if (name is null) {
            throw new ArgumentNullException(nameof(name));
        }

        lock (_gate) {
            ThrowIfDisposed();

            if (_members.TryGetValue(name, out var existing)) {
                return CastOrThrow<T>(existing, name);
            }

            _members[name] = value;
            Track(value);
            return value;
        }
    }


    private static T CastOrThrow<T>(object? value, string name)
        => value switch {
            T t => t,
            null when default(T) is null => default!,
            _ => throw new InvalidCastException($"Member '{name}' has type '{value?.GetType().FullName ?? "null"}' which is not assignable to '{typeof(T).FullName}'.")
        };


    private static void DisposeOrphan(object? value)
    {
        if (value is null) {
            return;
        }

        if (value is IAsyncDisposable ad) {
            try {
                ad.DisposeAsync().AsTask().GetAwaiter().GetResult();
            } catch {
                // Ignore exceptions in disposal
            }

        } else if (value is IDisposable d) {
            SafeDispose(d);
        }
    }

    /// <summary>
    /// Removes the member with the specified name from the bag.
    /// </summary>
    /// <param name="name">The member name to remove.</param>
    /// <param name="disposeMember">When true, disposes the removed value if it was tracked as IDisposable/IAsyncDisposable.</param>
    /// <returns>True if the member existed and was removed; otherwise, false.</returns>
    public bool Remove(string name, bool disposeMember = false)
    {
        lock (_gate) {
            ThrowIfDisposed();

            if (_members.TryGetValue(name, out var old)) {
                _members.Remove(name);
                Untrack(old, dispose: disposeMember);
                return true;
            }

            return false;
        }
    }


    /// <summary>
    /// Clears all members from the bag.
    /// </summary>
    /// <param name="disposeMembers">When true, disposes all tracked values that implement IDisposable/IAsyncDisposable.</param>
    public void Clear(bool disposeMembers = false)
    {
        lock (_gate) {
            ThrowIfDisposed();

            if (_members.Count == 0) {
                return;
            }

            // Untrack each current value, optionally disposing as requested
            foreach (var value in _members.Values) {
                Untrack(value, dispose: disposeMembers);
            }

            _members.Clear();
        }
    }


    ~SideDataBag()
        => Dispose();


    public void Dispose()
    {
        HashSet<IDisposable> toDispose;
        HashSet<IAsyncDisposable> toDisposeAsync;

        lock (_gate) {
            if (_disposed) {
                return;
            }
            _disposed = true;

            toDispose = new HashSet<IDisposable>(_owned, _owned.Comparer);
            toDisposeAsync = new HashSet<IAsyncDisposable>(_ownedAsync, _ownedAsync.Comparer);

            _members.Clear();
            _owned.Clear();
            _ownedAsync.Clear();
        }

        foreach (var d in toDispose) {
            SafeDispose(d);
        }

        // Finalizer/synchronous path: must block on async disposal.
        // If DisposeAsync() requires a SynchronizationContext, this may deadlock,
        // but there is no alternative in a finalizer. The try/catch prevents crashes.
        foreach (var ad in toDisposeAsync) {
            try {
                ad.DisposeAsync().AsTask().GetAwaiter().GetResult();
            } catch {
                // Ignore exceptions in disposal
            }
        }

        GC.SuppressFinalize(this);
    }

    
    public async ValueTask DisposeAsync()
    {
        HashSet<IDisposable> toDispose;
        HashSet<IAsyncDisposable> toDisposeAsync;

        lock (_gate) {
            if (_disposed) {
                return;
            }
            _disposed = true;

            toDispose = new HashSet<IDisposable>(_owned, _owned.Comparer);
            toDisposeAsync = new HashSet<IAsyncDisposable>(_ownedAsync, _ownedAsync.Comparer);

            _members.Clear();
            _owned.Clear();
            _ownedAsync.Clear();
        }

        foreach (var d in toDispose) {
            SafeDispose(d);
        }

        foreach (var ad in toDisposeAsync) {
            try {
                await ad.DisposeAsync().ConfigureAwait(false);
            } catch {
                // Ignore exceptions in disposal
            }
        }

        GC.SuppressFinalize(this);
    }

    
    private void Track(object? value)
    {
        if (value is null) {
            return;
        }

        // Prefer async disposal when available (avoid double-dispose if a type implements both)
        if (value is IAsyncDisposable ad) {
            _ownedAsync.Add(ad);
        } else if (value is IDisposable d) {
            _owned.Add(d);
        }
    }


    private void Untrack(object? value, bool dispose)
    {
        if (value is null) {
            return;
        }

        if (value is IAsyncDisposable ad && _ownedAsync.Remove(ad)) {
            if (dispose) {
                try {
                    ad.DisposeAsync().AsTask().GetAwaiter().GetResult();
                } catch {
                    // Ignore exceptions in disposal
                }
            }
        } else if (value is IDisposable d && _owned.Remove(d)) {
            if (dispose) {
                SafeDispose(d);
            }
        }
    }


    private static void SafeDispose(IDisposable d)
    {
        try {
            d.Dispose();
        } catch {
            // Ignore exceptions in disposal
        }
    }


    private void ThrowIfDisposed()
    {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(SideDataBag));
        }
    }


    private readonly HashSet<IAsyncDisposable> _ownedAsync = new(ReferenceEqualityComparer<IAsyncDisposable>.Instance);
    private readonly HashSet<IDisposable> _owned = new(ReferenceEqualityComparer<IDisposable>.Instance);
    private readonly Dictionary<string, object?> _members = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private bool _disposed;
}
