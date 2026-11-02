using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using SideData.Fixtures;
using SideData.TestData;

namespace SideData;

[ClassDataSource<TestDataFixture>(Shared = SharedType.PerClass)]
public class SideDataBagTests(TestDataFixture testDataFixture)
{
    [Test]
    public async Task TypedAccessors_TryGet_And_GetOrDefault_Work_AsExpected()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var contact = Fixture.ContactDataFaker.Generate();
        bag["Contact"] = contact;
        bag["NullRef"] = null;

        // TryGet success
        await Assert.That(bag.TryGet("Contact", out object? boxed)).IsTrue();
        await Assert.That(boxed).IsSameReferenceAs(contact);

        // TryGet wrong type
        await Assert.That(bag.TryGet<int>("Contact", out var wrong)).IsFalse();
        await Assert.That(wrong).IsEqualTo(default);

        // TryGet null into nullable/ref type succeeds
        await Assert.That(bag.TryGet<object?>("NullRef", out var nullObj)).IsTrue();
        await Assert.That(nullObj).IsNull();

        // GetOrDefault success
        var got = bag.GetOrDefault<object>("Contact");
        await Assert.That(got).IsSameReferenceAs(contact);

        // GetOrDefault missing -> returns provided default
        var def = new object();
        var gotDefault = bag.GetOrDefault<object>("Missing", def);
        await Assert.That(gotDefault).IsSameReferenceAs(def);

        // GetOrDefault incompatible -> returns provided default
        var def2 = 42;
        var gotDefault2 = bag.GetOrDefault<int>("Contact", def2);
        await Assert.That(gotDefault2).IsEqualTo(def2);

        // GetOrDefault when value is null and T accepts null -> returns default(T)
        var gotNullDefault = bag.GetOrDefault<object?>("NullRef", new object());
        await Assert.That(gotNullDefault).IsNull();
    }


    [Test]
    public async Task TypedAccessors_Throw_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        await Assert.That(() => bag.TryGet("X", out object? _)).ThrowsExactly<ObjectDisposedException>();
        await Assert.That(() => bag.GetOrDefault<object>("X")).ThrowsExactly<ObjectDisposedException>();
    }


    [Test]
    public async Task Remove_DoesNotDispose_ByDefault_But_Disposes_When_Requested()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();

        bag["S"] = sync;
        bag["A"] = async;

        // Remove without disposal should not dispose and should untrack
        await Assert.That(bag.Remove("S")).IsTrue();
        await Assert.That(sync.Disposed).IsFalse();

        // Removing missing returns false
        await Assert.That(bag.Remove("Missing")).IsFalse();

        // Remove with disposal should dispose
        await Assert.That(bag.Remove("A", disposeMember: true)).IsTrue();
        await Assert.That(async.Disposed).IsTrue();

        // Disposing bag afterwards should not touch already-removed items
        bag.Dispose();
        await Assert.That(sync.Disposed).IsFalse(); // still not disposed because it was untracked before bag disposal
    }


    [Test]
    public async Task Clear_Removes_All_Members_And_Optionally_Disposes()
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
        await Assert.That(s1.Disposed).IsFalse();
        await Assert.That(s2.Disposed).IsFalse();
        await Assert.That(a1.Disposed).IsFalse();

        // After clear, members should be gone
        await Assert.That(bag["s1"]).IsNull();
        await Assert.That(bag["s2"]).IsNull();
        await Assert.That(bag["a1"]).IsNull();

        // Re-add and clear with disposal
        bag["s1"] = s1;
        bag["s2"] = s2;
        bag["a1"] = a1;

        bag.Clear(disposeMembers: true);
        await Assert.That(s1.Disposed).IsTrue();
        await Assert.That(s2.Disposed).IsTrue();
        await Assert.That(a1.Disposed).IsTrue();
    }


    [Test]
    public async Task DisposeOnReplace_DisposesOldValue_WhenReplaced()
    {
        var host = new object();
        var bag = host.SideDataBag(disposeOnReplace: true);

        var d1 = new TestDisposable();
        var d2 = new TestDisposable();

        bag["Item"] = d1;
        await Assert.That(d1.Disposed).IsFalse();

        // Replace d1 with d2 - d1 should be disposed
        bag["Item"] = d2;
        await Assert.That(d1.Disposed).IsTrue();
        await Assert.That(d2.Disposed).IsFalse();

        // Dispose the bag - d2 should be disposed
        bag.Dispose();
        await Assert.That(d2.Disposed).IsTrue();
    }


    [Test]
    public async Task DisposeOnReplace_Mismatch_Throws()
    {
        var host = new object();

        // First call with disposeOnReplace: false
        var bag1 = host.SideDataBag(disposeOnReplace: false);

        // Second call with disposeOnReplace: true should throw
        var ex = await Assert.That(() => host.SideDataBag(disposeOnReplace: true)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("disposeOnReplace=False");
        await Assert.That(ex!.Message).Contains("disposeOnReplace=True");
    }


    [Test]
    public async Task DisposeAsync_DisposesAllTrackedMembers()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();

        bag["Sync"] = sync;
        bag["Async"] = async;

        await bag.DisposeAsync();

        await Assert.That(sync.Disposed).IsTrue();
        await Assert.That(async.Disposed).IsTrue();
    }


    [Test]
    public async Task DualDisposable_TrackedOnlyOnce_AndDisposedOnce()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var dual = new TestDualDisposable();
        bag["Dual"] = dual;

        // Dispose the bag
        bag.Dispose();

        // Should be disposed via async path (preferred), not both
        await Assert.That(dual.SyncDisposeCount).IsEqualTo(0);
        await Assert.That(dual.AsyncDisposeCount).IsEqualTo(1);
    }


    [Test]
    public async Task TypedIndexer_Set_And_Get_Works()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var value = new object();
        bag["key"] = value;
        var read = bag["key"];
        await Assert.That(read).IsSameReferenceAs(value);

        // Non-existent key returns null
        await Assert.That(bag["missing"]).IsNull();
    }


    [Test]
    public async Task GetDynamicMemberNames_ReturnsCurrentKeys()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["First"] = 1;
        bag["Second"] = 2;

        var names = bag.GetDynamicMemberNames().ToList();
        await Assert.That(names).Contains("First");
        await Assert.That(names).Contains("Second");
        await Assert.That(names.Count).IsEqualTo(2);

        // Clear and verify empty
        bag.Clear();
        var emptyNames = bag.GetDynamicMemberNames().ToList();
        await Assert.That(emptyNames).IsEmpty();
    }


    [Test]
    public async Task GetDynamicMemberNames_ThrowsAfterDispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["X"] = 1;
        bag.Dispose();

        await Assert.That(() => bag.GetDynamicMemberNames().ToList()).ThrowsExactly<ObjectDisposedException>();
    }


    #region Thread Safety Tests

    [Test]
    [Property("Category", "ThreadSafety")]
    public async Task SideDataBag_Concurrent_Read_Write_Same_Keys()
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

        await Assert.That(exceptions).IsEmpty();
    }

    [Test]
    [Property("Category", "ThreadSafety")]
    public async Task SideDataBag_Concurrent_Add_Different_Keys()
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

        await Assert.That(exceptions).IsEmpty();

        // Verify all keys were added
        for (int i = 0; i < 100; i++)
        {
            await Assert.That(bag.TryGet<int>($"key{i}", out var value)).IsTrue();
            await Assert.That(value).IsEqualTo(i);
        }
    }

    [Test]
    [Property("Category", "ThreadSafety")]
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
        }, TestContext.Current!.Execution.CancellationToken);

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
        }, TestContext.Current!.Execution.CancellationToken);

        await Task.WhenAll(writer, disposer);

        // Either write succeeded or got ObjectDisposedException - both are valid
        foreach (var ex in exceptions)
        {
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
        }
    }

    [Test]
    [Property("Category", "ThreadSafety")]
    public async Task SideDataBag_Concurrent_Remove_Same_Keys()
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
        await Assert.That(removeResults.Count(r => r)).IsEqualTo(10);

        // All keys should be gone
        for (int i = 0; i < 10; i++)
        {
            await Assert.That(bag[$"key{i}"]).IsNull();
        }
    }

    [Test]
    [Property("Category", "ThreadSafety")]
    public async Task SideDataBag_Concurrent_Clear()
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

        await Assert.That(exceptions).IsEmpty();

        // All members should be cleared
        var names = bag.GetDynamicMemberNames().ToList();
        await Assert.That(names).IsEmpty();
    }

    #endregion


    #region Dispose Idempotency Tests

    [Test]
    [Property("Category", "Disposal")]
    public async Task Dispose_Called_Twice_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var disposable = new TestDisposable();
        bag["Item"] = disposable;

        bag.Dispose();
        bag.Dispose();

        await Assert.That(disposable.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task DisposeAsync_Called_Twice_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var disposable = new TestAsyncDisposable();
        bag["Item"] = disposable;

        await bag.DisposeAsync();
        await bag.DisposeAsync();

        await Assert.That(disposable.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [Property("Category", "Disposal")]
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

        await Assert.That(sync.DisposeCount).IsEqualTo(1);
        await Assert.That(async.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [Property("Category", "Disposal")]
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

        await Assert.That(sync.DisposeCount).IsEqualTo(1);
        await Assert.That(async.DisposeCount).IsEqualTo(1);
    }

    #endregion


    #region ObjectDisposedException Coverage

    [Test]
    [Property("Category", "Disposal")]
    public async Task Remove_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        await Assert.That(() => bag.Remove("X")).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task Clear_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        await Assert.That(() => bag.Clear()).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task TypedIndexer_Get_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag["X"] = 1;
        bag.Dispose();

        await Assert.That(() => { var _ = bag["X"]; }).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task TypedIndexer_Set_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag.Dispose();

        await Assert.That(() => bag["X"] = 1).ThrowsExactly<ObjectDisposedException>();
    }

    #endregion


    #region TryGet Value Type Tests

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task TryGet_ValueType_Success()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = 42;

        await Assert.That(bag.TryGet<int>("number", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task TryGet_ValueType_WrongType_Returns_False()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = 42;

        await Assert.That(bag.TryGet<long>("number", out var value)).IsFalse();
        await Assert.That(value).IsEqualTo(default);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task TryGet_NullableValueType_Success()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = (int?)42;

        await Assert.That(bag.TryGet<int?>("number", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task TryGet_NullableValueType_Null_Success()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["number"] = (int?)null;

        await Assert.That(bag.TryGet<int?>("number", out var value)).IsTrue();
        await Assert.That(value).IsNull();
    }

    #endregion


    #region GetOrDefault with Null Edge Cases

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrDefault_NullValue_NonNullableValueType_Returns_Default()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["value"] = null;

        var result = bag.GetOrDefault<int>("value", 99);
        await Assert.That(result).IsEqualTo(99);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrDefault_NullValue_NullableValueType_Returns_Null()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["value"] = null;

        var result = bag.GetOrDefault<int?>("value", 99);
        await Assert.That(result).IsNull();
    }

    #endregion


    #region Clear Edge Cases

    [Test]
    [Property("Category", "Clear")]
    public async Task Clear_On_Empty_Bag_Does_Not_Throw()
    {
        var host = new object();
        var bag = host.SideDataBag();

        // Should not throw
        bag.Clear();
        bag.Clear(disposeMembers: true);

        var names = bag.GetDynamicMemberNames().ToList();
        await Assert.That(names).IsEmpty();
    }

    [Test]
    [Property("Category", "Clear")]
    public async Task Clear_Then_Dispose_Does_Not_Double_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var d1 = new TestDisposable();
        var d2 = new TestAsyncDisposable();

        bag["d1"] = d1;
        bag["d2"] = d2;

        bag.Clear(disposeMembers: true);
        await Assert.That(d1.Disposed).IsTrue();
        await Assert.That(d2.Disposed).IsTrue();

        // Reset counters via new instances to verify Dispose doesn't try to dispose again
        // (can't reset the counter on already-disposed items, but we can check they don't increment)
        var initialD1Count = d1.DisposeCount;
        var initialD2Count = d2.DisposeCount;

        bag.Dispose();

        // Counts should not increase because items were already untracked
        await Assert.That(d1.DisposeCount).IsEqualTo(initialD1Count);
        await Assert.That(d2.DisposeCount).IsEqualTo(initialD2Count);
    }

    #endregion


    #region GetOrAdd Tests

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Creates_And_Stores_When_Missing()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var calls = 0;
        var result = bag.GetOrAdd("key", () => { calls++; return 42; });

        await Assert.That(result).IsEqualTo(42);
        await Assert.That(calls).IsEqualTo(1);
        await Assert.That(bag.TryGet<int>("key", out var stored)).IsTrue();
        await Assert.That(stored).IsEqualTo(42);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Returns_Existing_Without_Invoking_Factory()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = 7;

        var calls = 0;
        var result = bag.GetOrAdd("key", () => { calls++; return 99; });

        await Assert.That(result).IsEqualTo(7);
        await Assert.That(calls).IsEqualTo(0);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Existing_WrongType_Throws_InvalidCast()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = "a string";

        await Assert.That(() => bag.GetOrAdd<int>("key", () => 1)).ThrowsExactly<InvalidCastException>();
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Existing_Null_Returns_Default_For_RefType()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = null;

        var calls = 0;
        var result = bag.GetOrAdd<object?>("key", () => { calls++; return new object(); });

        await Assert.That(result).IsNull();
        await Assert.That(calls).IsEqualTo(0);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Null_Arguments_Throw()
    {
        var host = new object();
        var bag = host.SideDataBag();

        await Assert.That(() => bag.GetOrAdd<int>(null!, () => 1)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => bag.GetOrAdd<int>("key", null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task GetOrAdd_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag.Dispose();

        await Assert.That(() => bag.GetOrAdd("key", () => 1)).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task GetOrAdd_Disposes_Orphan_When_Losing_Race()
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

        await Assert.That(result).IsSameReferenceAs(winner);
        await Assert.That(winner.Disposed).IsFalse();
        await Assert.That(loser.Disposed).IsTrue();
        await Assert.That(bag["key"]).IsSameReferenceAs(winner);
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task GetOrAdd_Disposes_Orphan_When_Disposed_During_Factory()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var orphan = new TestDisposable();

        await Assert.That(() => bag.GetOrAdd("key", () =>
        {
            bag.Dispose();
            return orphan;
        })).ThrowsExactly<ObjectDisposedException>();

        await Assert.That(orphan.Disposed).IsTrue();
    }

    [Test]
    [Property("Category", "ThreadSafety")]
    public async Task GetOrAdd_Concurrent_Same_Key_Returns_Single_Winner()
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
        foreach (var r in results)
        {
            await Assert.That(r).IsEqualTo(winner);
        }
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_With_Lazy_Initializes_Exactly_Once()
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

        foreach (var v in values)
        {
            await Assert.That(v).IsEqualTo(5);
        }
        await Assert.That(factoryCalls).IsEqualTo(1);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Eager_Stores_And_Returns_When_Missing()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var result = bag.GetOrAdd("key", 42);

        await Assert.That(result).IsEqualTo(42);
        await Assert.That(bag.TryGet<int>("key", out var stored)).IsTrue();
        await Assert.That(stored).IsEqualTo(42);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Eager_Returns_Existing()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = 7;

        var result = bag.GetOrAdd("key", 99);

        await Assert.That(result).IsEqualTo(7);
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Eager_Existing_WrongType_Throws_InvalidCast()
    {
        var host = new object();
        var bag = host.SideDataBag();

        bag["key"] = "a string";

        await Assert.That(() => bag.GetOrAdd("key", 1)).ThrowsExactly<InvalidCastException>();
    }

    [Test]
    [Property("Category", "TypedAccessors")]
    public async Task GetOrAdd_Eager_Null_Name_Throws()
    {
        var host = new object();
        var bag = host.SideDataBag();

        await Assert.That(() => bag.GetOrAdd(null!, 1)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task GetOrAdd_Eager_Throws_After_Dispose()
    {
        var host = new object();
        var bag = host.SideDataBag();
        bag.Dispose();

        await Assert.That(() => bag.GetOrAdd("key", 1)).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task GetOrAdd_Eager_Does_Not_Dispose_Losing_Value()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var winner = new TestDisposable();
        var loser = new TestDisposable();
        bag["key"] = winner;

        var result = bag.GetOrAdd("key", loser);

        await Assert.That(result).IsSameReferenceAs(winner);
        await Assert.That(loser.Disposed).IsFalse();
        await Assert.That(winner.Disposed).IsFalse();
    }

    [Test]
    [Property("Category", "Disposal")]
    public async Task GetOrAdd_Eager_Tracks_Stored_Disposable()
    {
        var host = new object();
        var bag = host.SideDataBag();

        var disposable = new TestDisposable();
        bag.GetOrAdd("key", disposable);

        bag.Dispose();
        await Assert.That(disposable.Disposed).IsTrue();
    }

    #endregion


    #region Dynamic Binding Tests

    // The C# binder has no syntax for deleting a member and routes string keys to the real indexer,
    // so these paths are only reachable through a call site the way another DLR language would use it

    [Test]
    public async Task Delete_Member_Removes_The_Entry_And_Rejects_A_Missing_One()
    {
        var bag = new SideDataBag();
        bag.GetOrAdd("A", 1);

        await Assert.That(TryDelete(bag, "A")).IsTrue();
        await Assert.That(bag.TryGet<int>("A", out _)).IsFalse();
        await Assert.That(TryDelete(bag, "A")).IsFalse();
    }

    [Test]
    public async Task Index_Binders_Read_And_Write_Entries_And_Reject_A_Missing_Key()
    {
        var bag = new SideDataBag();

        SetIndexed(bag, "A", 5);

        await Assert.That(GetIndexed(bag, "A")).IsEqualTo(5);
        await Assert.That(bag.GetOrDefault<int>("A")).IsEqualTo(5);
        await Assert.That(() => GetIndexed(bag, "Missing")).ThrowsExactly<MissingMemberException>();
    }

    private static bool TryDelete(object target, string name)
    {
        var site = CallSite<Action<CallSite, object>>.Create(new DeleteBinder(name));
        try {
            site.Target(site, target);
            return true;
        } catch (MissingMemberException) {
            return false;
        }
    }

    private static object? GetIndexed(object target, object key)
    {
        var site = CallSite<Func<CallSite, object, object, object?>>.Create(new IndexGetBinder());
        return site.Target(site, target, key);
    }

    private static void SetIndexed(object target, object key, object? value)
    {
        var site = CallSite<Func<CallSite, object, object, object?, object?>>.Create(new IndexSetBinder());
        site.Target(site, target, key, value);
    }

    private sealed class DeleteBinder(string name) : DeleteMemberBinder(name, ignoreCase: false)
    {
        public override DynamicMetaObject FallbackDeleteMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
            => errorSuggestion ?? new DynamicMetaObject(Expression.Throw(Expression.Constant(new MissingMemberException(Name))), target.Restrictions);
    }

    private sealed class IndexGetBinder() : GetIndexBinder(new CallInfo(1))
    {
        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject? errorSuggestion)
            => errorSuggestion ?? new DynamicMetaObject(Expression.Throw(Expression.Constant(new MissingMemberException("index")), typeof(object)), target.Restrictions);
    }

    private sealed class IndexSetBinder() : SetIndexBinder(new CallInfo(1))
    {
        public override DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject? errorSuggestion)
            => errorSuggestion ?? new DynamicMetaObject(Expression.Throw(Expression.Constant(new MissingMemberException("index")), typeof(object)), target.Restrictions);
    }

    #endregion


    private TestDataFixture Fixture { get; } = testDataFixture;
}
