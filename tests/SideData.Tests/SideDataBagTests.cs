using SideData.Fixtures;
using SideData.TestData;

namespace SideData;

public class SideDataBagTests(TestDataFixture testDataFixture) : IClassFixture<TestDataFixture>
{
    [Fact]
    public void TypedAccessors_TryGet_And_GetOrDefault_Work_AsExpected()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var contact = Fixture.ContactDataFaker.Generate();
        bag["Contact"] = contact;
        bag["NullRef"] = null;

        // TryGet success
        Assert.True(bag.TryGet("Contact", out object? boxed));
        Assert.Same(contact, boxed);

        // TryGet wrong type
        Assert.False(bag.TryGet<int>("Contact", out var wrong));
        Assert.Equal(default, wrong);

        // TryGet null into nullable/ref type succeeds
        Assert.True(bag.TryGet<object?>("NullRef", out var nullObj));
        Assert.Null(nullObj);

        // GetOrDefault success
        var got = bag.GetOrDefault<object>("Contact");
        Assert.Same(contact, got);

        // GetOrDefault missing -> returns provided default
        var def = new object();
        var gotDefault = bag.GetOrDefault<object>("Missing", def);
        Assert.Same(def, gotDefault);

        // GetOrDefault incompatible -> returns provided default
        var def2 = 42;
        var gotDefault2 = bag.GetOrDefault<int>("Contact", def2);
        Assert.Equal(def2, gotDefault2);

        // GetOrDefault when value is null and T accepts null -> returns default(T)
        var gotNullDefault = bag.GetOrDefault<object?>("NullRef", new object());
        Assert.Null(gotNullDefault);
    }


    [Fact]
    public void TypedAccessors_Throw_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag.TryGet("X", out object? _));
        Assert.Throws<ObjectDisposedException>(() => bag.GetOrDefault<object>("X"));
    }


    [Fact]
    public void Remove_DoesNotDispose_ByDefault_But_Disposes_When_Requested()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();

        bag["S"] = sync;
        bag["A"] = async;

        // Remove without disposal should not dispose and should untrack
        Assert.True(bag.Remove("S"));
        Assert.False(sync.Disposed);

        // Removing missing returns false
        Assert.False(bag.Remove("Missing"));

        // Remove with disposal should dispose
        Assert.True(bag.Remove("A", disposeMember: true));
        Assert.True(async.Disposed);

        // Disposing bag afterwards should not touch already-removed items
        bag.Dispose();
        Assert.False(sync.Disposed); // still not disposed because it was untracked before bag disposal
    }


    [Fact]
    public void Clear_Removes_All_Members_And_Optionally_Disposes()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var s1 = new TestDisposable();
        var s2 = new TestDisposable();
        var a1 = new TestAsyncDisposable();

        bag["s1"] = s1;
        bag["s2"] = s2;
        bag["a1"] = a1;

        // Clear with no disposal
        bag.Clear();
        Assert.False(s1.Disposed);
        Assert.False(s2.Disposed);
        Assert.False(a1.Disposed);

        // After clear, members should be gone
        Assert.Null(bag["s1"]);
        Assert.Null(bag["s2"]);
        Assert.Null(bag["a1"]);

        // Re-add and clear with disposal
        bag["s1"] = s1;
        bag["s2"] = s2;
        bag["a1"] = a1;

        bag.Clear(disposeMembers: true);
        Assert.True(s1.Disposed);
        Assert.True(s2.Disposed);
        Assert.True(a1.Disposed);
    }


    [Fact]
    public void DisposeOnReplace_DisposesOldValue_WhenReplaced()
    {
        var host = new object();
        var bag = host.SideDataBag(disposeOnReplace: true);

        var d1 = new TestDisposable();
        var d2 = new TestDisposable();

        bag["Item"] = d1;
        Assert.False(d1.Disposed);

        // Replace d1 with d2 - d1 should be disposed
        bag["Item"] = d2;
        Assert.True(d1.Disposed);
        Assert.False(d2.Disposed);

        // Dispose the bag - d2 should be disposed
        bag.Dispose();
        Assert.True(d2.Disposed);
    }


    [Fact]
    public void DisposeOnReplace_Mismatch_Throws()
    {
        var host = new object();

        // First call with disposeOnReplace: false
        var bag1 = host.SideDataBag(disposeOnReplace: false);

        // Second call with disposeOnReplace: true should throw
        var ex = Assert.Throws<InvalidOperationException>(() => host.SideDataBag(disposeOnReplace: true));
        Assert.Contains("disposeOnReplace=False", ex.Message);
        Assert.Contains("disposeOnReplace=True", ex.Message);
    }


    [Fact]
    public async Task DisposeAsync_DisposesAllTrackedMembers()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();

        bag["Sync"] = sync;
        bag["Async"] = async;

        await bag.DisposeAsync();

        Assert.True(sync.Disposed);
        Assert.True(async.Disposed);
    }


    [Fact]
    public void DualDisposable_TrackedOnlyOnce_AndDisposedOnce()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var dual = new TestDualDisposable();
        bag["Dual"] = dual;

        // Dispose the bag
        bag.Dispose();

        // Should be disposed via async path (preferred), not both
        Assert.Equal(0, dual.SyncDisposeCount);
        Assert.Equal(1, dual.AsyncDisposeCount);
    }


    [Fact]
    public void TypedIndexer_Set_And_Get_Works()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var value = new object();
        bag["key"] = value;
        var read = bag["key"];
        Assert.Same(value, read);

        // Non-existent key returns null
        Assert.Null(bag["missing"]);
    }


    [Fact]
    public void GetDynamicMemberNames_ReturnsCurrentKeys()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["First"] = 1;
        bag["Second"] = 2;

        var names = bag.GetDynamicMemberNames().ToList();
        Assert.Contains("First", names);
        Assert.Contains("Second", names);
        Assert.Equal(2, names.Count);

        // Clear and verify empty
        bag.Clear();
        var emptyNames = bag.GetDynamicMemberNames().ToList();
        Assert.Empty(emptyNames);
    }


    [Fact]
    public void GetDynamicMemberNames_ThrowsAfterDispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["X"] = 1;
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag.GetDynamicMemberNames().ToList());
    }


    #region Thread Safety Tests

    [Fact]
    [Trait("Category", "ThreadSafety")]
    public void SideDataBag_Concurrent_Read_Write_Same_Keys()
    {
        var host = new object();
        var bag = host.SideDataBag();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Pre-populate with some data
        for (int i = 0; i < 10; i++)
        {
            bag[$"key{i}"] = i;
        }

        Parallel.For(0, 100, i =>
        {
            try
            {
                // Mix of reads and writes
                if (i % 2 == 0)
                {
                    var key = $"key{i % 10}";
                    bag[key] = i;
                }
                else
                {
                    var key = $"key{i % 10}";
                    var val = bag[key];
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    [Trait("Category", "ThreadSafety")]
    public void SideDataBag_Concurrent_Add_Different_Keys()
    {
        var host = new object();
        var bag = host.SideDataBag();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        Parallel.For(0, 100, i =>
        {
            try
            {
                bag[$"key{i}"] = i;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);

        // Verify all keys were added
        for (int i = 0; i < 100; i++)
        {
            Assert.True(bag.TryGet<int>($"key{i}", out var value));
            Assert.Equal(i, value);
        }
    }

    [Fact]
    [Trait("Category", "ThreadSafety")]
    public async Task SideDataBag_Concurrent_Dispose_While_Writing()
    {
        var host = new object();
        var bag = host.SideDataBag();
        var writeStarted = new ManualResetEventSlim(false);
        var disposeStarted = new ManualResetEventSlim(false);
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var writer = Task.Run(() =>
        {
            try
            {
                writeStarted.Set();
                for (int i = 0; i < 1000; i++)
                {
                    bag[$"key{i}"] = i;
                    if (i == 10) disposeStarted.Wait();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }, TestContext.Current.CancellationToken);

        var disposer = Task.Run(() =>
        {
            try
            {
                writeStarted.Wait();
                disposeStarted.Set();
                bag.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(writer, disposer);

        // Either write succeeded or got ObjectDisposedException - both are valid
        Assert.All(exceptions, ex => Assert.IsType<ObjectDisposedException>(ex));
    }

    [Fact]
    [Trait("Category", "ThreadSafety")]
    public void SideDataBag_Concurrent_Remove_Same_Keys()
    {
        var host = new object();
        var bag = host.SideDataBag();

        // Pre-populate
        for (int i = 0; i < 10; i++)
        {
            bag[$"key{i}"] = i;
        }

        var removeResults = new System.Collections.Concurrent.ConcurrentBag<bool>();

        Parallel.For(0, 100, i =>
        {
            var result = bag.Remove($"key{i % 10}");
            removeResults.Add(result);
        });

        // Only 10 removals should succeed (one per key)
        Assert.Equal(10, removeResults.Count(r => r));

        // All keys should be gone
        for (int i = 0; i < 10; i++)
        {
            Assert.Null(bag[$"key{i}"]);
        }
    }

    [Fact]
    [Trait("Category", "ThreadSafety")]
    public void SideDataBag_Concurrent_Clear()
    {
        var host = new object();
        var bag = host.SideDataBag();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Pre-populate
        for (int i = 0; i < 100; i++)
        {
            bag[$"key{i}"] = i;
        }

        Parallel.For(0, 10, _ =>
        {
            try
            {
                bag.Clear();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);

        // All members should be cleared
        var names = bag.GetDynamicMemberNames().ToList();
        Assert.Empty(names);
    }

    #endregion


    #region Dispose Idempotency Tests

    [Fact]
    [Trait("Category", "Disposal")]
    public void Dispose_Called_Twice_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var disposable = new TestDisposable();
        bag["Item"] = disposable;

        bag.Dispose();
        bag.Dispose();

        Assert.Equal(1, disposable.DisposeCount);
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public async Task DisposeAsync_Called_Twice_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var disposable = new TestAsyncDisposable();
        bag["Item"] = disposable;

        await bag.DisposeAsync();
        await bag.DisposeAsync();

        Assert.Equal(1, disposable.DisposeCount);
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public async Task Dispose_Then_DisposeAsync_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();
        bag["Sync"] = sync;
        bag["Async"] = async;

        bag.Dispose();
        await bag.DisposeAsync();

        Assert.Equal(1, sync.DisposeCount);
        Assert.Equal(1, async.DisposeCount);
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public async Task DisposeAsync_Then_Dispose_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();
        bag["Sync"] = sync;
        bag["Async"] = async;

        await bag.DisposeAsync();
        bag.Dispose();

        Assert.Equal(1, sync.DisposeCount);
        Assert.Equal(1, async.DisposeCount);
    }

    #endregion


    #region ObjectDisposedException Coverage

    [Fact]
    [Trait("Category", "Disposal")]
    public void Remove_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag.Remove("X"));
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void Clear_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag.Clear());
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void TypedIndexer_Get_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => { var _ = bag["X"]; });
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void TypedIndexer_Set_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag["X"] = 1);
    }

    #endregion


    #region TryGet Value Type Tests

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void TryGet_ValueType_Success()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = 42;

        Assert.True(bag.TryGet<int>("number", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void TryGet_ValueType_WrongType_Returns_False()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = 42;

        Assert.False(bag.TryGet<long>("number", out var value));
        Assert.Equal(default, value);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void TryGet_NullableValueType_Success()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = (int?)42;

        Assert.True(bag.TryGet<int?>("number", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void TryGet_NullableValueType_Null_Success()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = (int?)null;

        Assert.True(bag.TryGet<int?>("number", out var value));
        Assert.Null(value);
    }

    #endregion


    #region GetOrDefault with Null Edge Cases

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrDefault_NullValue_NonNullableValueType_Returns_Default()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["value"] = null;

        var result = bag.GetOrDefault<int>("value", 99);
        Assert.Equal(99, result);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrDefault_NullValue_NullableValueType_Returns_Null()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["value"] = null;

        var result = bag.GetOrDefault<int?>("value", 99);
        Assert.Null(result);
    }

    #endregion


    #region Clear Edge Cases

    [Fact]
    [Trait("Category", "Clear")]
    public void Clear_On_Empty_Bag_Does_Not_Throw()
    {
        var host = new object();
        var bag = host.SideDataBag();

        // Should not throw
        bag.Clear();
        bag.Clear(disposeMembers: true);

        var names = bag.GetDynamicMemberNames().ToList();
        Assert.Empty(names);
    }

    [Fact]
    [Trait("Category", "Clear")]
    public void Clear_Then_Dispose_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var d1 = new TestDisposable();
        var d2 = new TestAsyncDisposable();

        bag["d1"] = d1;
        bag["d2"] = d2;

        bag.Clear(disposeMembers: true);
        Assert.True(d1.Disposed);
        Assert.True(d2.Disposed);

        // Reset counters via new instances to verify Dispose doesn't try to dispose again
        // (can't reset the counter on already-disposed items, but we can check they don't increment)
        var initialD1Count = d1.DisposeCount;
        var initialD2Count = d2.DisposeCount;

        bag.Dispose();

        // Counts should not increase because items were already untracked
        Assert.Equal(initialD1Count, d1.DisposeCount);
        Assert.Equal(initialD2Count, d2.DisposeCount);
    }

    #endregion


    #region GetOrAdd Tests

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Creates_And_Stores_When_Missing()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var calls = 0;
        var result = bag.GetOrAdd("key", () => { calls++; return 42; });

        Assert.Equal(42, result);
        Assert.Equal(1, calls);
        Assert.True(bag.TryGet<int>("key", out var stored));
        Assert.Equal(42, stored);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Returns_Existing_Without_Invoking_Factory()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = 7;

        var calls = 0;
        var result = bag.GetOrAdd("key", () => { calls++; return 99; });

        Assert.Equal(7, result);
        Assert.Equal(0, calls);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Existing_WrongType_Throws_InvalidCast()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = "a string";

        Assert.Throws<InvalidCastException>(() => bag.GetOrAdd<int>("key", () => 1));
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Existing_Null_Returns_Default_For_RefType()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = null;

        var calls = 0;
        var result = bag.GetOrAdd<object?>("key", () => { calls++; return new object(); });

        Assert.Null(result);
        Assert.Equal(0, calls);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Null_Arguments_Throw()
    {
        var host = new object();
        var bag = host.SideDataBag();

        Assert.Throws<ArgumentNullException>(() => bag.GetOrAdd<int>(null!, () => 1));
        Assert.Throws<ArgumentNullException>(() => bag.GetOrAdd<int>("key", null!));
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void GetOrAdd_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag.GetOrAdd("key", () => 1));
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void GetOrAdd_Disposes_Orphan_When_Losing_Race()
    {
        var host = new object();
        var bag = host.SideDataBag();

        // Simulate a lost race: factory inserts a competing value before returning.
        var winner = new TestDisposable();
        var loser = new TestDisposable();

        var result = bag.GetOrAdd("key", () =>
        {
            // Another caller wins while the factory runs.
            bag["key"] = winner;
            return loser;
        });

        Assert.Same(winner, result);
        Assert.False(winner.Disposed);
        Assert.True(loser.Disposed);
        Assert.Same(winner, bag["key"]);
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void GetOrAdd_Disposes_Orphan_When_Disposed_During_Factory()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var orphan = new TestDisposable();

        Assert.Throws<ObjectDisposedException>(() => bag.GetOrAdd("key", () =>
        {
            bag.Dispose();
            return orphan;
        }));

        Assert.True(orphan.Disposed);
    }

    [Fact]
    [Trait("Category", "ThreadSafety")]
    public void GetOrAdd_Concurrent_Same_Key_Returns_Single_Winner()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var created = new System.Collections.Concurrent.ConcurrentBag<int>();

        var results = new int[100];
        Parallel.For(0, 100, i =>
        {
            results[i] = bag.GetOrAdd("key", () =>
            {
                var v = Environment.CurrentManagedThreadId;
                created.Add(v);
                return v;
            });
        });

        var winner = bag.GetOrDefault<int>("key");
        Assert.All(results, r => Assert.Equal(winner, r));
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_With_Lazy_Initializes_Exactly_Once()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var factoryCalls = 0;
        Lazy<int> MakeLazy() => new(() => { Interlocked.Increment(ref factoryCalls); return 5; }, LazyThreadSafetyMode.ExecutionAndPublication);

        var values = new int[100];
        Parallel.For(0, 100, i =>
        {
            var lazy = bag.GetOrAdd("key", MakeLazy);
            values[i] = lazy.Value;
        });

        Assert.All(values, v => Assert.Equal(5, v));
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Eager_Stores_And_Returns_When_Missing()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var result = bag.GetOrAdd("key", 42);

        Assert.Equal(42, result);
        Assert.True(bag.TryGet<int>("key", out var stored));
        Assert.Equal(42, stored);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Eager_Returns_Existing()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = 7;

        var result = bag.GetOrAdd("key", 99);

        Assert.Equal(7, result);
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Eager_Existing_WrongType_Throws_InvalidCast()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = "a string";

        Assert.Throws<InvalidCastException>(() => bag.GetOrAdd("key", 1));
    }

    [Fact]
    [Trait("Category", "TypedAccessors")]
    public void GetOrAdd_Eager_Null_Name_Throws()
    {
        var host = new object();
        var bag = host.SideDataBag();

        Assert.Throws<ArgumentNullException>(() => bag.GetOrAdd(null!, 1));
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void GetOrAdd_Eager_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag.GetOrAdd("key", 1));
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void GetOrAdd_Eager_Does_Not_Dispose_Losing_Value()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var winner = new TestDisposable();
        var loser = new TestDisposable();
        bag["key"] = winner;

        var result = bag.GetOrAdd("key", loser);

        Assert.Same(winner, result);
        Assert.False(loser.Disposed);
        Assert.False(winner.Disposed);
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void GetOrAdd_Eager_Tracks_Stored_Disposable()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var disposable = new TestDisposable();
        bag.GetOrAdd("key", disposable);

        bag.Dispose();
        Assert.True(disposable.Disposed);
    }

    #endregion


    private TestDataFixture Fixture { get; } = testDataFixture;
}
