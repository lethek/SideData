using SideData.Fixtures;
using SideData.TestData;

namespace SideData;

public class SideDataTests : IClassFixture<TestDataFixture>
{
    public SideDataTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


    [Fact]
    public void SideData_ForEachHostObject_IsUniquePerHostObject()
    {
        var user1 = Fixture.UserFaker.Generate();
        var user1FirstSideData = user1.SideData();
        var user1SecondSideData = user1.SideData();
        Assert.Same(user1FirstSideData, user1SecondSideData);

        var user2 = Fixture.UserFaker.Generate();
        var user2FirstSideData = user2.SideData();
        var user2SecondSideData = user2.SideData();
        Assert.Same(user2FirstSideData, user2SecondSideData);

        //SideData for user1 should be different to the SideData for user2
        Assert.NotSame(user1FirstSideData, user2FirstSideData);
    }


    [Fact]
    public void SetAndGet_MultipleSideData_ReturnsSameValues()
    {
        var user = Fixture.UserFaker.Generate();
        var primaryContact = Fixture.ContactDataFaker.Generate();
        var secondaryContact = Fixture.ContactDataFaker.Generate();

        user.SideData().PrimaryContact = primaryContact;
        user.SideData().SecondaryContact = secondaryContact;

        Assert.NotSame(primaryContact, secondaryContact);
        Assert.Same(primaryContact, user.SideData().PrimaryContact);
        Assert.Same(secondaryContact, user.SideData().SecondaryContact);
    }


    [Fact]
    public void ExplicitDispose_DisposesTrackedMembers()
    {
        var host = new object();
        dynamic bag = host.SideData();

        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();

        bag.Sync = sync;
        bag.Async = async;

        //Dispose the bag explicitly
        ((IDisposable)bag).Dispose();

        Assert.True(sync.Disposed);
        Assert.True(async.Disposed);

        //Further operations on bag should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => { var _ = bag.Sync; });
    }

    
    [Fact]
    public void Finalizer_DisposesTrackedMembers_WhenHostIsCollected()
    {
        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();
        WeakReference hostRef;

        CreateHostAndAttach(out hostRef, sync, async);

        //Ensure host is collected
        ForceFullGc();
        Assert.False(hostRef.IsAlive);

        //Give finalizers a chance to run and Dispose the bag-owned members
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!(sync.Disposed && async.Disposed) && DateTime.UtcNow < deadline) {
            ForceFullGc();
            Thread.Sleep(10);
        }

        Assert.True(sync.Disposed);
        Assert.True(async.Disposed);
    }

    
    private static void CreateHostAndAttach(out WeakReference hostRef, TestDisposable sync, TestAsyncDisposable async)
    {
        var host = new object();
        hostRef = new WeakReference(host);

        //Attach side-data and store disposables inside the bag
        dynamic bag = host.SideData();
        bag.Sync = sync;
        bag.Async = async;

        //Drop the only strong reference to host (outside ConditionalWeakTable)
        //After this method returns, only the CWT holds host key; GC can collect it.
        GC.KeepAlive(bag); // ensure bag stays alive until method end
    }

    
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
    public void DynamicIndexer_Set_And_Get_StringKey_Works()
    {
        var host = new object();
        dynamic dyn = host.SideData();

        var value = new object();
        dyn["key"] = value; // TrySetIndex
        var read = dyn["key"]; // TryGetIndex
        Assert.Same(value, read);
    }

    
    [Fact]
    public void DynamicIndexer_InvalidKeyType_Throws()
    {
        var host = new object();
        dynamic dyn = host.SideData();

        // Using an int key should cause the dynamic binder to fail because TrySetIndex returns false
        Assert.ThrowsAny<Exception>(() => dyn[123] = 5);
        Assert.ThrowsAny<Exception>(() => { var _ = dyn[123]; });
    }
    
    
    private static void ForceFullGc()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    
    private TestDataFixture Fixture { get; }
}
