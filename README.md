# SideData
Attach data to any object, for the lifetime of that object.

SideData lets you associate arbitrary data with any reference-type object without modifying the object itself. The attached data lives as long as the host object; when the host is garbage collected, its side data is collected too.

## Features

- Automatic cleanup: attached data is collected when the host object is GC’d
- Two APIs: ergonomic dynamic API and a strongly-typed API
- Dynamic members and indexer: bag.Property and bag["key"]
- Optional ownership and disposal of stored IDisposable/IAsyncDisposable (including opt-in dispose-on-replace)
- Thread-safe operations
- Lightweight (built on ConditionalWeakTable)

## Requirements

- Target frameworks: netstandard2.0, netstandard2.1, net8.0
- Reference types only (value types are not supported as hosts)

## Installation

```bash
# Package is available on NuGet
dotnet add package SideData
```

## Quick start

### Dynamic API (recommended for most scenarios)

```csharp
using SideData;

var obj = new { Name = "Some object" };

// Attach and use data via dynamic members
dynamic bag = obj.SideData(); // or obj.SideData(disposeOnReplace: true)
bag.Shape = "Rectangle";
bag.NumberOfEdges = 4;
bag.EdgeLengths = new[] { 6, 4, 6, 4 };

Console.WriteLine($"A {bag.Shape} has {bag.NumberOfEdges} sides");
// Output: A Rectangle has 4 sides
```

Dynamic indexer support is available for string keys:

```csharp
dynamic bag = obj.SideData();
bag["primary"] = new object();
var value = bag["primary"]; // same as dynamic property semantics
```

### Strongly-typed API

Use SideDataBag when you prefer explicit, compile-time friendly access.

```csharp
using SideData;

var obj = new object();
var bag = obj.SideDataBag(); // or obj.SideDataBag(disposeOnReplace: true)

// Indexer access
bag["Answer"] = 42;
int? answer = bag.GetOrDefault<int>("Answer"); // 42

// Typed helper methods
if (bag.TryGet("Answer", out int value))
{
    Console.WriteLine(value);
}

// Get an existing value or create and store one on first access
int port = bag.GetOrAdd("Port", () => ComputeDefaultPort());

// Eager overload when the value is already in hand
int retries = bag.GetOrAdd("Retries", 3);

// Remove/Clear (do not dispose by default)
bag.Remove("Answer");
bag.Clear();
```

For exactly-once initialization under concurrency, store a `Lazy<T>` (the factory itself may run more than once when callers race):

```csharp
var lazy = bag.GetOrAdd("Parsed", () => new Lazy<Result>(Parse));
Result result = lazy.Value; // Parse runs at most once
```

## Ownership and disposal

SideDataBag can track IDisposable and IAsyncDisposable values stored in it. By default, the bag owns these values and will dispose them when the bag is disposed or when the host object is garbage collected.

- Setting or replacing a member: the previously stored value is untracked. If the bag (or dynamic API via obj.SideData/SideDataBag) was created with disposeOnReplace: true, the replaced value will be disposed when it’s replaced. By default (disposeOnReplace: false), replacement does not dispose.
- Remove(name, disposeMember = false): removes a single member. Pass disposeMember: true to dispose the removed value if it implements IDisposable/IAsyncDisposable.
- Clear(disposeMembers = false): removes all members. Pass disposeMembers: true to dispose all removed values that implement IDisposable/IAsyncDisposable.
- Disposing the bag (Dispose or DisposeAsync): disposes all tracked IDisposable/IAsyncDisposable values owned by the bag and prevents further use.
- When the host object is collected by the GC: the associated bag is finalized and will best-effort dispose tracked IDisposable/IAsyncDisposable values.

Notes:
- Exceptions thrown during disposal are swallowed to ensure cleanup continues.
- After disposal, any operation on the bag throws ObjectDisposedException.

## Thread-safety

- Each bag serializes its operations internally; individual operations are thread-safe.
- Compound operations are not atomic; if you need multi-step atomicity, use your own synchronization.

## API reference (quick overview)

Extension methods:
- dynamic ObjectExtensions.SideData<T>(this T obj, bool disposeOnReplace = false) where T : class
- SideDataBag ObjectExtensions.SideDataBag<T>(this T obj, bool disposeOnReplace = false) where T : class

Dynamic usage (examples):
- bag.PropertyName = value;
- var x = bag.PropertyName;
- bag["key"] = value; var y = bag["key"];

Typed usage (SideDataBag):
- object? this[string name] { get; set; }
- IEnumerable<string> GetDynamicMemberNames()
- bool TryGet<T>(string name, out T? value)
- T? GetOrDefault<T>(string name, T? defaultValue = default)
- T GetOrAdd<T>(string name, Func<T> factory) // factory runs outside the lock and may run more than once on races; losing values are disposed if disposable. Throws InvalidCastException on a name clash with an incompatible type.
- T GetOrAdd<T>(string name, T value) // stores value if absent; a losing value is returned untouched and never disposed. Throws InvalidCastException on a name clash with an incompatible type.
- bool Remove(string name, bool disposeMember = false)
- void Clear(bool disposeMembers = false)
- void Dispose(); ValueTask DisposeAsync();

Low-level attachment API (advanced):
- dynamic SideDataAttachment.Get(object hostObject)
- dynamic SideDataAttachment.GetOrAdd(object hostObject, Func<dynamic>? factory = null)
- bool SideDataAttachment.TryGet(object hostObject, out dynamic value)
- void SideDataAttachment.Remove(object hostObject) // disposes the bag if present and detaches it

## Tips and performance notes

- Keys are case-sensitive and compared using StringComparer.Ordinal.
- Host objects are keyed by reference identity; attaching data does not prevent host collection.
- Prefer the typed API in hot paths to avoid dynamic binder overhead.

## Examples and tests

See the unit tests in tests/SideData.Tests for more examples and expected behavior.

## License

MIT

