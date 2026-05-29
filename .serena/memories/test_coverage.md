# Test Coverage

## Test Suite Summary
- **Total tests**: 78 unique tests (156 total executions across net10.0 and net8.0)
- **Test files**: 
  - `SideDataTests.cs` - Tests for dynamic API (`.SideData()`) - 13 tests
  - `SideDataBagTests.cs` - Tests for SideDataBag class (`.SideDataBag()`) - 49 tests
  - `SideDataAttachmentTests.cs` - Tests for SideDataAttachment class - 16 tests
- **Test frameworks**: xUnit v3.2.2 with Microsoft Testing Platform v2

## Coverage Categories (using Trait attributes)

### ThreadSafety (7 tests)
- Concurrent read/write operations on SideDataBag
- Concurrent add operations with different keys
- Concurrent dispose while writing
- Concurrent remove operations on same keys
- Concurrent clear operations
- Concurrent GetOrAdd on same key returns a single winner

### Disposal (22 tests)
- Dispose idempotency (calling Dispose/DisposeAsync multiple times)
- Mixed disposal (Dispose then DisposeAsync, and vice versa)
- DisposeOnReplace behavior via different code paths (TrySetMember, TrySetIndex)
- ObjectDisposedException thrown after disposal for all operations
- Explicit disposal, async disposal, and finalizer-based disposal
- GetOrAdd orphan disposal (lost race, disposed-during-factory); eager overload does not dispose a losing caller-owned value but tracks a stored disposable

### TypedAccessors (18 tests)
- TryGet with value types (int, long, nullable types)
- GetOrDefault with null values and non-nullable types
- Type checking and conversion
- GetOrAdd (factory and eager overloads): create-when-missing, return-existing, wrong-type throws InvalidCastException, null member handling, arg validation, Lazy<T> exactly-once pattern

### Clear (2 tests)
- Clear on empty bag (edge case)
- Clear followed by Dispose (no double-dispose)

### Internals (8 tests)
- ReferenceEqualityComparer behavior
- Reference vs structural equality
- Identity hash code generation

## Full Coverage Achieved

All identified coverage gaps have been filled:
1. ✅ Thread safety tests for concurrent access to SideDataBag
2. ✅ Dispose idempotency tests (double Dispose, double DisposeAsync, mixed calls)
3. ✅ DisposeOnReplace via dynamic member set (TrySetMember) and dynamic indexer (TrySetIndex)
4. ✅ ObjectDisposedException for all operations after dispose
5. ✅ TryGet success path for value types
6. ✅ GetOrDefault with null values and non-nullable types
7. ✅ ReferenceEqualityComparer direct tests
8. ✅ Clear edge cases (empty bag, followed by Dispose)

## Test Helper Classes

Located in `tests/SideData.Tests/TestData/`:
- `TestDisposable.cs` - IDisposable with dispose counter
- `TestAsyncDisposable.cs` - IAsyncDisposable with dispose counter
- `TestDualDisposable.cs` - Implements both IDisposable and IAsyncDisposable
- `User.cs`, `ContactData.cs` - Domain models for Bogus-generated test data

## Running Specific Test Categories

```bash
# Run all thread safety tests
dotnet test --filter-namespace "SideData" | grep -A 100 "ThreadSafety"

# Run all disposal tests
dotnet test --filter-namespace "SideData" | grep -A 100 "Disposal"

# Note: MTP doesn't support filtering by Trait directly, use class/method filters instead
```

## Test Quality Metrics

- All public API methods have coverage
- Internal ReferenceEqualityComparer has comprehensive tests
- Thread safety verified with Parallel.For stress tests
- Disposal paths tested for both sync (IDisposable) and async (IAsyncDisposable)
- Edge cases tested (null values, empty collections, post-dispose operations)
- No known coverage gaps
