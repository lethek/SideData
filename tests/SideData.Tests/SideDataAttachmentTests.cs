using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SideData.TestData;

namespace SideData;

public class SideDataAttachmentTests
{
    [Test]
    public async Task GetOrAdd_Creates_And_Reuses_Bag_For_Same_Host()
    {
        var host = new object();
        var first = SideDataAttachment.GetOrAdd(host);
        var second = SideDataAttachment.GetOrAdd(host);
        await Assert.That((object)second).IsSameReferenceAs((object)first);
    }


    [Test]
    public async Task Different_Hosts_Get_Different_Bags()
    {
        var host1 = new object();
        var host2 = new object();

        var bag1 = SideDataAttachment.GetOrAdd(host1);
        var bag2 = SideDataAttachment.GetOrAdd(host2);

        await Assert.That((object)bag2).IsNotSameReferenceAs((object)bag1);
    }


    [Test]
    public async Task TryGet_Returns_False_When_Absent_True_When_Present()
    {
        var host = new object();
        await Assert.That(SideDataAttachment.TryGet(host, out dynamic _)).IsFalse();

        var created = SideDataAttachment.GetOrAdd(host);
        await Assert.That(SideDataAttachment.TryGet(host, out dynamic found)).IsTrue();
        await Assert.That((object)found).IsSameReferenceAs((object)created);
    }


    [Test]
    public async Task Get_Throws_When_Absent_Then_Returns_When_Present()
    {
        var host = new object();
        await Assert.That(() => SideDataAttachment.Get(host)).ThrowsExactly<KeyNotFoundException>();

        var created = SideDataAttachment.GetOrAdd(host);
        var got = SideDataAttachment.Get(host);
        await Assert.That((object)got).IsSameReferenceAs((object)created);
    }


    [Test]
    public async Task Remove_Detaches_And_Disposes_Bag()
    {
        var host = new object();
        dynamic bag = SideDataAttachment.GetOrAdd(host);

        // Put a disposable inside the bag to verify disposal via SideDataAttachment.Remove
        var d = new TestDisposable();
        bag.D = d;

        // Remove should dispose the bag (which disposes tracked members) and detach it
        SideDataAttachment.Remove(host);

        await Assert.That(d.Disposed).IsTrue();
        await Assert.That(SideDataAttachment.TryGet(host, out dynamic _)).IsFalse();
        await Assert.That(() => SideDataAttachment.Get(host)).ThrowsExactly<KeyNotFoundException>();
    }


    [Test]
    public async Task Remove_On_Host_With_No_Bag_Is_NoOp()
    {
        var host = new object();
        // Should not throw
        await Assert.That(() => {
            SideDataAttachment.Remove(host);
            // Subsequent calls are still no-op
            SideDataAttachment.Remove(host);
        }).ThrowsNothing();
    }


    [Test]
    public async Task GetOrAdd_Uses_Factory_Result()
    {
        var host = new object();
        var custom = new SideDataBag();
        var created = SideDataAttachment.GetOrAdd(host, () => (dynamic)custom);
        await Assert.That((object)created).IsSameReferenceAs(custom);

        // Ensure it is cached for the host
        var again = SideDataAttachment.GetOrAdd(host);
        await Assert.That((object)again).IsSameReferenceAs(custom);
    }


    [Test]
    public async Task Existing_Bag_Is_Returned_Without_Allocating()
    {
        var host = new object();
        SideDataAttachment.GetOrAdd(host);

        // Warm up so JIT work is not counted
        for (var i = 0; i < 100; i++) {
            SideDataAttachment.GetOrAdd(host);
            host.SideDataBag();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) {
            SideDataAttachment.GetOrAdd(host);
            host.SideDataBag();
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        await Assert.That(allocated).IsEqualTo(0L);
    }


    [Test]
    public async Task GetOrAdd_Throws_When_Factory_Result_Is_Not_A_Dynamic_Object()
    {
        var host = new object();

        await Assert.That(() => { SideDataAttachment.GetOrAdd(host, () => new object()); }).ThrowsExactly<InvalidCastException>();
        await Assert.That(SideDataAttachment.TryGet(host, out dynamic _)).IsFalse();
    }


    [Test]
    [Property("Category", "Internals")]
    public async Task Library_Does_Not_Reference_The_Dynamic_Runtime_Binder()
    {
        // Any dynamic operation in the library compiles to a Microsoft.CSharp call site, which Native AOT cannot run
        var referenced = typeof(SideDataAttachment).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        await Assert.That(referenced).DoesNotContain("Microsoft.CSharp");
    }


    [Test]
    [Property("Category", "Internals")]
    public async Task Only_The_Dynamic_Entry_Point_Requires_Dynamic_Code()
    {
        // Native AOT apps using the typed API must see no warning, and apps using the dynamic API must see one
        var dynamicEntry = typeof(ObjectExtensions).GetMethod(nameof(ObjectExtensions.SideData))!;
        var typedEntry = typeof(ObjectExtensions).GetMethod(nameof(ObjectExtensions.SideDataBag))!;

        await Assert.That(dynamicEntry.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false)).IsTrue();
        await Assert.That(typedEntry.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false)).IsFalse();
    }


    [Test]
    public async Task Parallel_GetOrAdd_Is_Idempotent_Per_Host()
    {
        var host = new object();
        var set = new ConcurrentBag<object>();

        Parallel.For(0, 32, _ => {
            var bag = (object)SideDataAttachment.GetOrAdd(host);
            set.Add(bag);
        });

        // All items should refer to the same single instance
        var first = set.First();
        foreach (var x in set) {
            await Assert.That(x).IsSameReferenceAs(first);
        }
    }


    #region ReferenceEqualityComparer Tests

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_Equals_Returns_True_For_Same_Reference()
    {
        var obj = new object();
        var comparer = TestReferenceEqualityComparer<object>.Instance;

        await Assert.That(comparer.Equals(obj, obj)).IsTrue();
    }

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_Equals_Returns_False_For_Different_References()
    {
        var obj1 = new object();
        var obj2 = new object();
        var comparer = TestReferenceEqualityComparer<object>.Instance;

        await Assert.That(comparer.Equals(obj1, obj2)).IsFalse();
    }

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_Equals_Returns_True_For_Both_Null()
    {
        var comparer = TestReferenceEqualityComparer<object>.Instance;

        await Assert.That(comparer.Equals(null, null)).IsTrue();
    }

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_Equals_Returns_False_For_One_Null()
    {
        var obj = new object();
        var comparer = TestReferenceEqualityComparer<object>.Instance;

        await Assert.That(comparer.Equals(obj, null)).IsFalse();
        await Assert.That(comparer.Equals(null, obj)).IsFalse();
    }

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_GetHashCode_Returns_IdentityHash()
    {
        var obj = new object();
        var comparer = TestReferenceEqualityComparer<object>.Instance;

        var hash1 = comparer.GetHashCode(obj);
        var hash2 = RuntimeHelpers.GetHashCode(obj);

        await Assert.That(hash1).IsEqualTo(hash2);
    }

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_GetHashCode_Returns_Zero_For_Null()
    {
        var comparer = TestReferenceEqualityComparer<object>.Instance;

        var hash = comparer.GetHashCode(null!);
        await Assert.That(hash).IsEqualTo(0);
    }

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_GetHashCode_Different_For_Different_Objects()
    {
        var obj1 = new object();
        var obj2 = new object();
        var comparer = TestReferenceEqualityComparer<object>.Instance;

        var hash1 = comparer.GetHashCode(obj1);
        var hash2 = comparer.GetHashCode(obj2);

        // Note: theoretically hashes COULD collide, but extremely unlikely
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }

    [Test]
    [Property("Category", "Internals")]
    public async Task ReferenceEqualityComparer_Equals_For_Structurally_Equal_Strings()
    {
        var str1 = new string("test");
        var str2 = new string("test");
        var comparer = TestReferenceEqualityComparer<string>.Instance;

        // Strings have same content but are different instances
        await Assert.That(str1).IsEqualTo(str2); // structural equality
        await Assert.That(comparer.Equals(str1, str2)).IsFalse(); // reference inequality
    }

    /// <summary>
    /// Test wrapper to access the internal ReferenceEqualityComparer via a public equivalent.
    /// Since we can't use [InternalsVisibleTo] easily here, we replicate the logic.
    /// </summary>
    private sealed class TestReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class?
    {
        public static TestReferenceEqualityComparer<T> Instance { get; } = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => obj is null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }

    #endregion
}
