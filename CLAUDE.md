# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                                                    # Build entire solution
dotnet test                                                     # Run all tests
dotnet test --framework net8.0                                  # Run tests against a specific TFM
dotnet test --treenode-filter "/*/*/SideDataBagTests/*"         # Run tests in a specific class
dotnet test --treenode-filter "/*/*/*/Clear_Throws_After_Dispose"  # Run a single test method
dotnet test --treenode-filter "/*/*/*/*[Category=Disposal]"     # Run tests with a category
dotnet pack -c Release                                          # Create NuGet package
```

**Note:** TUnit runs on Microsoft Testing Platform and filters with `--treenode-filter`, not VSTest's `--filter`. The path is `/Assembly/Namespace/Class/Method`, and `*` matches any segment:
- **Class:** `/*/*/SideDataBagTests/*`
- **Method:** `/*/*/*/MethodName`
- **Namespace:** `/*/SideData/*/*`
- **Category:** `/*/*/*/*[Category=Disposal]`, negated with `[Category!=Disposal]`
- **Combining:** `|` for OR, adjacent `[...][...]` brackets for AND

Categories come from `[Property("Category", "...")]` on the test method. The ones in use are `Disposal`, `TypedAccessors`, `ThreadSafety`, `Clear` and `Internals`.

Tests use **TUnit 1.65.31** and target `net10.0` and `net8.0`. The library multi-targets `net8.0`, `netstandard2.1`, and `netstandard2.0`.

## Architecture

SideData attaches arbitrary data to any .NET reference-type object using `ConditionalWeakTable`, so attached data is automatically garbage-collected with its host object.

### Two API layers (both in `SideData` namespace, single `using SideData;` import)

1. **Dynamic API** — `obj.SideData()` returns a `dynamic` `SideDataBag` (which extends `DynamicObject`) for property-style access (`bag.Foo = 42`) and indexer access (`bag["key"] = value`).

2. **Strongly-typed API** — `obj.SideDataBag()` returns the same `SideDataBag` but as its concrete type, providing `TryGet<T>`, `GetOrDefault<T>`, `Remove`, `Clear`, etc.

Both extension methods live in `ObjectExtensions`. They call through to `SideDataAttachment`, which manages the static `ConditionalWeakTable` mapping host objects to their `SideDataBag` instances.

### Key implementation details

- **`SideDataAttachment`** (`SideDataAttachment.cs`) — Static class holding the `ConditionalWeakTable<object, SideDataBag>`. Provides `Get`, `GetOrAdd`, `TryGet`, `Remove`. This is the low-level attachment point.
- **`SideDataBag`** (`SideDataBag.cs`) — `DynamicObject` subclass that stores members in a dictionary. Implements `IDisposable`/`IAsyncDisposable`. Tracks owned disposable values and disposes them on bag disposal or host GC (via finalizer). Thread-safe via internal lock (`_gate`).
- **`ReferenceEqualityComparer<T>`** (`Internals/`) — Used to key the `ConditionalWeakTable` by reference identity.
- **Ownership/disposal** — `SideDataBag` can optionally dispose replaced values (`disposeOnReplace: true`). It always disposes tracked `IDisposable`/`IAsyncDisposable` on `Dispose()`/`DisposeAsync()` or finalization.

### Versioning

Uses **GitVersion** in ContinuousDeployment mode. Version is injected during CI build.

## Code Style

- 4-space indentation, CRLF line endings
- Allman braces for methods and types; K&R for control flow (`else`/`catch`/`finally` on same line as closing brace)
- Prefer `var`, expression-bodied members, and pattern matching
- Private fields use `_camelCase` prefix
- No `this.` qualification
