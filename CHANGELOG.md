# SideData Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Entries for v1.1.0 and earlier were reconstructed from the release history, so they summarise each
release rather than record it as it happened.

## [Unreleased]

### Changed

- Raised the `Microsoft.Bcl.AsyncInterfaces` dependency from 10.0.8 to 10.0.14.

## [1.1.3] - 2026-11-02

### Changed

- `obj.SideData()` is marked `RequiresDynamicCode` on `net8.0`, so calling it from a Native AOT app reports `IL3050`.
- The `net8.0` assembly is marked trimmable and AOT compatible (`IsAotCompatible`).

### Fixed

- Publishing an app that uses SideData with Native AOT no longer reports `IL3053` for the SideData assembly.

## [1.1.2] - 2026-11-01

### Changed

- `obj.SideData()`, `obj.SideDataBag()` and `SideDataAttachment.GetOrAdd` no longer allocate when the host already has a bag.

### Fixed

- `obj.SideDataBag()` and `SideDataAttachment.GetOrAdd` no longer throw on first use under Native AOT.

## [1.1.1] - 2026-09-02

### Changed

- `obj.SideDataBag()` throws `InvalidOperationException` naming the attached type when the host carries a custom `IDynamicMetaObjectProvider`, rather than surfacing a `RuntimeBinderException`.

## [1.1.0] - 2026-05-29

### Added

- `SideDataBag.GetOrAdd<T>(string, Func<T>)`. The factory runs outside the lock, so under contention it may run more than once and the losing value is disposed.
- `SideDataBag.GetOrAdd<T>(string, T)`, which stores the value only when the name is absent and never disposes a losing caller-owned value.

### Changed

- Raised the `Microsoft.Bcl.AsyncInterfaces` dependency from 9.0.8 to 10.0.8.

## [1.0.0] - 2026-02-16

### Added

- `SideDataBag`, a `DynamicObject` holding the attached members, with `TryGet<T>`, `GetOrDefault<T>`, `Remove`, `Clear`, a string indexer and `GetDynamicMemberNames()`.
- `IDisposable` and `IAsyncDisposable` on `SideDataBag`, which disposes tracked values on disposal or when the host is collected.
- `disposeOnReplace`, which additionally disposes a value when it is overwritten.
- `SideDataAttachment`, the low-level attachment API, with `Get`, `GetOrAdd`, `TryGet` and `Remove`.
- `ObjectExtensions.SideDataBag<T>(this T, bool)`, the statically typed counterpart to `SideData()`.
- `net8.0` target.

### Changed

- **Breaking:** `obj.SideData()` returns a `SideDataBag` rather than an `ExpandoObject`. Member and indexer access is unchanged; the bag adds disposal tracking.
- **Breaking:** replaced the `net7.0` target with `net8.0`. The `netstandard2.0` and `netstandard2.1` targets are unchanged.

### Removed

- **Breaking:** the `SideData.LowLevel` namespace and `TypedSideData<T>`, which keyed attached values by type rather than by name. Use `obj.SideDataBag()` with the name-based accessors instead. No deprecation release preceded this; it was a rewrite.

## [0.1.0] - 2022-11-24

### Added

- `obj.SideData()`, attaching a dynamic `ExpandoObject` to any reference type for that object's lifetime, backed by `ConditionalWeakTable`.
- `SideData.LowLevel.TypedSideData<T>`, keying attached values by type, with `Set`, `Add`, `TryAdd`, `Remove`, `Get`, `GetOrDefault`, `GetOrAdd` and `TryGet`.
- Targets `netstandard2.0`, `netstandard2.1` and `net7.0`.

[Unreleased]: https://github.com/lethek/SideData/compare/v1.1.3...HEAD
[1.1.3]: https://github.com/lethek/SideData/compare/v1.1.2...v1.1.3
[1.1.2]: https://github.com/lethek/SideData/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/lethek/SideData/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/lethek/SideData/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/lethek/SideData/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/lethek/SideData/releases/tag/v0.1.0
