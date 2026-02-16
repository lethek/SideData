# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                                    # Build entire solution
dotnet test                                     # Run all tests
dotnet test --filter-class "FullyQualifiedName.ClassName"  # Run tests in a specific class
dotnet test --filter-method "FullyQualifiedName.ClassName.TestName"     # Run a single test by name
dotnet test -f net8.0                           # Run tests against a specific TFM
dotnet pack -c Release                          # Create NuGet package
```

Tests use **xUnit v3** (with **Microsoft.Testing.Platform v2**) and target `net8.0` and `net10.0`. The library itself multi-targets `netstandard2.0`, `netstandard2.1`, and `net7.0`.

## Architecture

SideData is a small library that attaches arbitrary data to any .NET reference-type object using `ConditionalWeakTable`, so attached data is automatically garbage-collected with its host object.

### Two API layers

1. **High-level dynamic API** (`SideData` namespace) — `obj.SideData()` returns a dynamic `ExpandoObject` for property-style access. This is a thin wrapper that calls through to the typed API.

2. **Low-level typed API** (`SideData.LowLevel` namespace) — `obj.TypedSideData()` returns a `TypedSideData<T>` struct providing type-safe access (`Get<T>`, `Set<T>`, `GetOrAdd<T>`, etc.). Stores one value per type per host object.

### Source files (all under `src/SideData/`)

- `ObjectExtensions.cs` — The `SideData<T>()` extension method (high-level dynamic API)
- `LowLevel/ObjectExtensions.cs` — The `TypedSideData<T>()` extension method (low-level typed API)
- `LowLevel/TypedSideData.cs` — The `TypedSideData<TK>` struct with all storage operations. Contains `#if` preprocessor directives for polyfilling APIs across target frameworks (e.g., `AddOrUpdate` on net7.0+ vs lock-based fallback on netstandard2.0)

### Key implementation details

- Storage uses a static generic pattern: `AttachedData<T>` is a private static class inside `TypedSideData<TK>` holding a `ConditionalWeakTable<TK, RecordBox<T>>`. The `RecordBox<T>` wrapper enables storing value types.
- The two `ObjectExtensions` classes live in different namespaces (`SideData` vs `SideData.LowLevel`) — consumers import one or both via `using` directives.
- Versioning uses **GitVersion** in ContinuousDeployment mode.

## Code Style

- 4-space indentation, CRLF line endings
- K&R braces for control flow (`else`/`catch`/`finally` on same line as closing brace); Allman braces for methods and types
- Prefer `var`, expression-bodied members, and pattern matching
- Private fields use `_camelCase` prefix
