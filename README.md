# SideData
Attach data to any object, for the lifetime of that object.

SideData allows you to attach arbitrary data to any .NET object without modifying the object itself. The attached data is automatically garbage-collected when the host object is collected, preventing memory leaks.

## Installation

```bash
dotnet add package SideData
```

## Quick Start

```csharp
using SideData;

var obj = new { Name = "Some object" };
obj.SideData().Shape = "Rectangle";
obj.SideData().NumberOfEdges = 4;
obj.SideData().EdgeLengths = new int[] { 6, 4, 6, 4 };

Console.WriteLine($"A {obj.SideData().Shape} has {obj.SideData().NumberOfEdges} sides");
// Output: A Rectangle has 4 sides
```

## When to Use SideData

SideData is particularly useful when:

- **Extending 3rd-party classes**: You need to store state in an extension method but can't modify the original class
- **Runtime metadata**: Attaching computed values, caching, or UI-specific data to domain objects
- **Avoiding inheritance**: You want to add data to objects without creating wrapper classes or using inheritance
- **Temporary associations**: You need to associate data with an object for a limited scope

## API Overview

### High-Level Dynamic API

The `SideData()` extension method returns a dynamic `ExpandoObject` for convenient property-style access:

```csharp
using SideData;

var user = new User { Username = "alice" };

// Attach dynamic properties
user.SideData().LastLogin = DateTime.UtcNow;
user.SideData().LoginCount = 42;
user.SideData().Preferences = new { Theme = "Dark", Language = "en" };

// Access them later
Console.WriteLine($"Last login: {user.SideData().LastLogin}");
Console.WriteLine($"Total logins: {user.SideData().LoginCount}");
```

### Low-Level Typed API

For type-safe access with more control, use the `TypedSideData()` extension method:

```csharp
using SideData.LowLevel;

var user = new User { Username = "bob" };

// Type-safe storage (one value per type per object)
user.TypedSideData().Set<string>("cached-token");
user.TypedSideData().Set<int>(100);
user.TypedSideData().Set<LoginMetadata>(new LoginMetadata { ... });

// Type-safe retrieval
var token = user.TypedSideData().Get<string>();
var count = user.TypedSideData().GetOrDefault<int>(0);

// Check and add
if (user.TypedSideData().TryAdd<string>("another-token")) {
    Console.WriteLine("Token added");
}

// GetOrAdd pattern (lazy initialization)
var metadata = user.TypedSideData().GetOrAdd(u => new LoginMetadata {
    Username = u.Username,
    CreatedAt = DateTime.UtcNow
});
```

### Available Methods (TypedSideData)

- `Set<T>(T value)` - Sets or replaces side-data of type T
- `Add<T>(T value)` - Adds side-data, throws if already exists
- `TryAdd<T>(T value)` - Attempts to add, returns success status
- `Get<T>()` - Gets side-data, throws if not found
- `GetOrDefault<T>(T defaultValue)` - Gets side-data or returns default
- `GetOrAdd<T>(Func<TK, T> factory)` - Gets existing or creates new side-data
- `TryGet<T>(out T value)` - Attempts to get side-data
- `Remove<T>()` - Removes side-data of type T

## Examples

### Extension Method State

```csharp
public static class StringExtensions
{
    public static string Reverse(this string str)
    {
        // Cache the reversed string as side-data
        return str.SideData().GetOrAdd(s => {
            char[] chars = s.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        });
    }
}

var text = "hello";
var reversed1 = text.Reverse(); // Computes and caches
var reversed2 = text.Reverse(); // Returns cached value
```

### Metadata Attachment

```csharp
// Attach metadata to objects from a 3rd-party library
var externalObject = thirdPartyLib.CreateObject();

externalObject.SideData().ProcessedAt = DateTime.UtcNow;
externalObject.SideData().ProcessedBy = "WorkerService";
externalObject.SideData().RetryCount = 0;

// Later in your code
if (externalObject.SideData().RetryCount > 3) {
    Console.WriteLine("Max retries exceeded");
}
```

### UI-Specific Data

```csharp
// Attach UI state to domain models without polluting them
var product = productRepository.GetById(123);

product.SideData().IsSelected = true;
product.SideData().ViewCount = 5;
product.SideData().LastViewedAt = DateTime.UtcNow;
```

## Important Notes

### Memory Management

- Side-data is stored using `ConditionalWeakTable`, which means it's automatically garbage-collected when the host object is collected
- No memory leaks occur even if you forget to remove side-data
- The host object must be a reference type (class)

### IDisposable Warning

⚠️ **SideData does NOT automatically dispose attached `IDisposable` objects.** If you attach disposable resources, you must handle their disposal manually:

```csharp
// NOT recommended - disposal won't happen automatically
obj.SideData().Stream = new FileStream(...);

// Better approach - dispose explicitly
var stream = obj.SideData().Stream as IDisposable;
stream?.Dispose();

// Or avoid attaching disposable resources as side-data
```

### Thread Safety

- All operations on `TypedSideData` are thread-safe
- The dynamic `SideData()` API is thread-safe for storage, but concurrent modifications to the same `ExpandoObject` properties are not synchronized

### Per-Type Storage

With the typed API, you can only store **one value per type** per object:

```csharp
obj.TypedSideData().Set<string>("first");
obj.TypedSideData().Set<string>("second"); // Replaces "first"

var result = obj.TypedSideData().Get<string>(); // Returns "second"
```

If you need multiple values of the same type, use the dynamic API or wrap values in different types.

## Framework Support

- .NET Standard 2.0
- .NET Standard 2.1
- .NET 7.0+

## License

MIT License - see LICENSE file for details

## Contributing

Issues and pull requests are welcome at [https://github.com/lethek/SideData](https://github.com/lethek/SideData)
