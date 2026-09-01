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

- **`SideDataAttachment`** (`SideDataAttachment.cs`) — Static class holding the `ConditionalWeakTable<object, IDynamicMetaObjectProvider>`. Provides `Get`, `GetOrAdd`, `TryGet`, `Remove`. This is the low-level attachment point. The value type is `IDynamicMetaObjectProvider`, not `SideDataBag`, **deliberately**: a caller can attach their own dynamic object (an `ExpandoObject`, say) via `SideDataAttachment.GetOrAdd(host, () => (dynamic)expando)` and reach it through `obj.SideData()`. Do not "simplify" this to `SideDataBag`; it would delete a working feature. `obj.SideDataBag()` is the one path that cannot serve a custom bag, and it throws `InvalidOperationException` naming the attached type.
- **`SideDataBag`** (`SideDataBag.cs`) — `DynamicObject` subclass that stores members in a dictionary. Implements `IDisposable`/`IAsyncDisposable`. Tracks owned disposable values and disposes them on bag disposal or host GC (via finalizer). Thread-safe via internal lock (`_gate`).
- **`ReferenceEqualityComparer<T>`** (`Internals/`) — Used to key the `ConditionalWeakTable` by reference identity.
- **Ownership/disposal** — `SideDataBag` can optionally dispose replaced values (`disposeOnReplace: true`). It always disposes tracked `IDisposable`/`IAsyncDisposable` on `Dispose()`/`DisposeAsync()` or finalization.

## Versioning and releases

GitVersion computes the version during the CI build; a local build produces a placeholder. `GitVersion.yml`
uses **GitVersion 6** naming, so `mode: ContinuousDelivery` there is what v5 called `ContinuousDeployment`.
Reading it with v5 silently produces a different number.

Untagged commits on `main` publish a `-ci.N` prerelease automatically. A `v*` tag publishes the stable
release. Both go to nuget.org and neither can be recalled.

Two skills cover this, and they do not overlap:

- **`/changelog`** — after landing a change that could reach package consumers. It decides whether the
  change earns a `CHANGELOG.md` entry at all, then writes it under `## [Unreleased]`. Use it whenever you
  are about to touch `CHANGELOG.md` outside a release, and do not hand-write entries from memory: the
  rules about what ships, which heading applies, and how a line is worded live there.
- **`/release`** — when cutting a release. It sizes the SemVer bump from the diff, promotes
  `## [Unreleased]`, commits and tags. It stops before the push that publishes, every time.

`CHANGELOG.md` follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). Repo-only changes
(tests, CI, `.claude/**`, this file) never get an entry.

## Code Style

- 4-space indentation, CRLF line endings
- Allman braces for methods and types; K&R for control flow (`else`/`catch`/`finally` on same line as closing brace)
- Prefer `var`, expression-bodied members, and pattern matching
- Private fields use `_camelCase` prefix
- No `this.` qualification
