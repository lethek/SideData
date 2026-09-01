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

2. **Strongly-typed API** — `obj.SideDataBag()` returns the same `SideDataBag` but as its concrete type, providing `TryGet<T>`, `GetOrDefault<T>`, `GetOrAdd<T>`, `Remove`, `Clear`, etc.

Both extension methods live in `ObjectExtensions`. They call through to `SideDataAttachment`, which manages the static `ConditionalWeakTable` mapping host objects to their attached bags.

Asking for a bag with a `disposeOnReplace` that disagrees with the one already attached throws `InvalidOperationException`. The flag is fixed for the bag's lifetime; the first call to attach wins.

### Key implementation details

- **`SideDataAttachment`** (`SideDataAttachment.cs`) — Static class holding the `ConditionalWeakTable<object, IDynamicMetaObjectProvider>`. Provides `Get`, `GetOrAdd`, `TryGet`, `Remove`. This is the low-level attachment point. The value type is `IDynamicMetaObjectProvider`, not `SideDataBag`, **deliberately**: a caller can attach their own dynamic object (an `ExpandoObject`, say) via `SideDataAttachment.GetOrAdd(host, () => (dynamic)expando)` and reach it through `obj.SideData()`. Do not "simplify" this to `SideDataBag`; it would delete a working feature. `obj.SideDataBag()` is the one path that cannot serve a custom bag, and it throws `InvalidOperationException` naming the attached type.
- **`SideDataBag`** (`SideDataBag.cs`) — `DynamicObject` subclass that stores members in a dictionary. Implements `IDisposable`/`IAsyncDisposable`. Tracks owned disposable values and disposes them on bag disposal or host GC (via finalizer). Thread-safe via internal lock (`_gate`).
- **`ReferenceEqualityComparer<T>`** (`Internals/`) — Backs the `_owned` and `_ownedAsync` `HashSet`s in `SideDataBag` (`SideDataBag.cs:503-504`), so a stored value is tracked by identity rather than by its own `Equals`. It is **not** used for the `ConditionalWeakTable`, which is reference-keyed inherently and takes no comparer. It exists because `System.Collections.Generic.ReferenceEqualityComparer` is .NET 5+ only and this library still targets `netstandard2.0`.
- **Ownership/disposal** — `SideDataBag` can optionally dispose replaced values (`disposeOnReplace: true`). It always disposes tracked `IDisposable`/`IAsyncDisposable` on `Dispose()`/`DisposeAsync()` or finalization.

### Gotchas worth knowing before you change anything

- **`GetOrAdd` overload resolution.** A delegate argument always binds to `GetOrAdd<T>(string, Func<T>)` and gets *invoked*, not stored. Storing a delegate needs an explicit type argument: `bag.GetOrAdd<Func<int>>(name, cb)`. Adding another overload here risks silently recapturing existing calls.
- **`GetOrAdd` is typed-API only.** `obj.SideData().GetOrAdd(name, () => x)` is a compile error (CS1977): a lambda cannot be an argument to a dynamically dispatched call.
- **`dynamic` hides breaking changes.** Members reached through `obj.SideData()` bind at runtime, so renaming or removing one produces no build error in a consumer, and none here either. Judge API breaks from the source, not from whether anything failed to compile.
- **TUnit assertions cannot bind `dynamic`.** In tests, cast to `(object)` first: `await Assert.That((object)found).IsSameReferenceAs((object)created)`. Since most of this API returns `dynamic`, this comes up constantly.

## Versioning and releases

GitVersion computes the version during the CI build; a local build produces a placeholder. `GitVersion.yml`
uses **GitVersion 6** naming, so `mode: ContinuousDelivery` there is what v5 called `ContinuousDeployment`.
Reading it with v5 silently produces a different number.

Untagged commits on `main` publish a `-ci.N` prerelease automatically. A `v*` tag publishes the stable
release. Both go to nuget.org and neither can be recalled.

### Keep the changelog in lockstep with the code

**`CHANGELOG.md` is updated as part of the work, not written up at release time.** Finishing a distinct
unit of work that touched shipped code means adding its `## [Unreleased]` entry before you commit it, open
the PR, or report the task done. A feature, a bug fix, a behaviour change and a dependency bump each get
their entry when they land, in the same commit as the code where practical.

A task that changed something consumers receive and left `CHANGELOG.md` untouched is not finished. Treat
the entry the way you treat a test: part of the change, not a follow-up.

Invoke **`/changelog`** to do it. Do not hand-write entries from memory; the rules about which of the six
headings applies and how a line is worded live in that skill. "No entry needed" is a valid outcome it will
reach on its own, so run it rather than deciding for yourself that a change is too small. That skill is
user-level and shared across projects, not in this repo, so it carries no knowledge of what SideData
ships. The table below is the project-specific half of it. Keep it accurate.

#### What ships in this repo

| Path | Entry? | Notes |
|---|---|---|
| `src/SideData/**/*.cs` | Yes, if the public API or its observable behaviour changed | Internal refactors that change nothing observable get no entry |
| `src/SideData/SideData.csproj` | Yes, for dependency versions, target frameworks, `Description`, `PackageTags` | These ship even though no `.cs` file moved |
| `README.md` | Only if it corrects something wrong | `PackageReadmeFile` is set, so it is the nuget.org landing page. Documenting existing behaviour is not a change to the software. |
| `tests/**`, `.github/**`, `CHANGELOG.md`, `CLAUDE.md`, `.claude/**`, `GitVersion.yml`, `global.json`, `*.slnx`, `.gitignore` | No | Repo-only |

The library-specific trap: members reached through `obj.SideData()` bind at runtime, so removing or
renaming one breaks consumers with no build error here or in their code. Size those from the source.

### Cutting a release

Invoke **`/release`**. It sizes the SemVer bump from the diff, promotes `## [Unreleased]`, commits and
tags, and stops before the push that publishes, every time. If `## [Unreleased]` is empty or stale when it
runs, the lockstep rule above was not followed and the release is being reconstructed from memory.

`CHANGELOG.md` follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/).

## Code Style

- 4-space indentation, CRLF line endings
- Allman braces for methods and types; K&R for control flow (`else`/`catch`/`finally` on same line as closing brace)
- Prefer `var`, expression-bodied members, and pattern matching
- Private fields use `_camelCase` prefix
- No `this.` qualification
