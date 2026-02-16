# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                                                    # Build entire solution
dotnet test                                                     # Run all tests
dotnet test --filter-class "SideData.SideDataTests"            # Run tests in a specific class (requires fully qualified name)
dotnet test --filter-method "SideData.SideDataTests.Remove_DoesNotDispose_ByDefault_But_Disposes_When_Requested"  # Run a single test method (fully qualified)
dotnet test --filter-namespace "SideData"                      # Run tests in a namespace
dotnet test --framework net8.0                                 # Run tests against a specific TFM
dotnet test --list-tests                                       # List all available tests without running them
dotnet pack -c Release                                         # Create NuGet package
```

**Note:** xUnit v3 with MTP uses different filter syntax than VSTest:
- **Class filtering:** `--filter-class "Namespace.ClassName"` (requires fully qualified name)
- **Method filtering:** `--filter-method "Namespace.ClassName.MethodName"` (requires fully qualified name)
- **Namespace filtering:** `--filter-namespace "NamespaceName"`
- **Negative filters:** `--filter-not-class`, `--filter-not-method`, `--filter-not-namespace`
- **Multiple values:** `--filter-class "ClassA" "ClassB"` (space-separated, not repeated flags)
- **Wildcards:** Support for `*` at beginning/end of filter values

Tests use **xUnit v3.2.2** with **Microsoft Testing Platform v2** and target `net10.0` and `net8.0`. The library multi-targets `net8.0`, `netstandard2.1`, and `netstandard2.0`.

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
