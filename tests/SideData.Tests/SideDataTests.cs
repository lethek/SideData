using SideData.Fixtures;
using SideData.TestData;

namespace SideData;

public class SideDataTests(TestDataFixture testDataFixture) : IClassFixture<TestDataFixture>
{
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

        // SideData for user1 should be different to the SideData for user2
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

        // Dispose the bag explicitly
        ((IDisposable)bag).Dispose();

        Assert.True(sync.Disposed);
        Assert.True(async.Disposed);

        // Further operations on bag should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => { var _ = bag.Sync; });
    }

    
    [Fact]
    public void Finalizer_DisposesTrackedMembers_WhenHostIsCollected()
    {
        var sync = new TestDisposable();
        var async = new TestAsyncDisposable();
        WeakReference hostRef;

        CreateHostAndAttach(out hostRef, sync, async);

        // Ensure host is collected
        ForceFullGc();
        Assert.False(hostRef.IsAlive);

        // Give finalizers a chance to run and Dispose the bag-owned members
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

        // Attach side-data and store disposables inside the bag
        dynamic bag = host.SideData();
        bag.Sync = sync;
        bag.Async = async;

        // Drop the only strong reference to host (outside ConditionalWeakTable)
        // After this method returns, only the CWT holds host key; GC can collect it.
        GC.KeepAlive(bag); // ensure bag stays alive until method end
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


    [Fact]
    public void DisposeOnReplace_Mismatch_Throws()
    {
        var host = new object();

        // First call with disposeOnReplace: false
        var bag1 = host.SideData(disposeOnReplace: false);

        // Second call with disposeOnReplace: true should throw
        var ex = Assert.Throws<InvalidOperationException>(() => host.SideData(disposeOnReplace: true));
        Assert.Contains("disposeOnReplace=False", ex.Message);
        Assert.Contains("disposeOnReplace=True", ex.Message);

        // Same with typed API
        var host2 = new object();
        var bag2 = host2.SideDataBag(disposeOnReplace: true);
        var ex2 = Assert.Throws<InvalidOperationException>(() => host2.SideDataBag(disposeOnReplace: false));
        Assert.Contains("disposeOnReplace=True", ex2.Message);
        Assert.Contains("disposeOnReplace=False", ex2.Message);
    }


    [Fact]
    public void GetDynamicMemberNames_ReturnsCurrentKeys()
    {
        var host = new object();
        dynamic dyn = host.SideData();

        dyn.First = 1;
        dyn.Second = 2;

        var names = ((SideDataBag)dyn).GetDynamicMemberNames().ToList();
        Assert.Contains("First", names);
        Assert.Contains("Second", names);
        Assert.Equal(2, names.Count);

        // Clear and verify empty
        ((SideDataBag)dyn).Clear();
        var emptyNames = ((SideDataBag)dyn).GetDynamicMemberNames().ToList();
        Assert.Empty(emptyNames);
    }


    #region DisposeOnReplace via Dynamic Paths

    [Fact]
    [Trait("Category", "Disposal")]
    public void DisposeOnReplace_Via_TrySetMember_Disposes_Old_Value()
    {
        var host = new object();
        dynamic bag = host.SideData(disposeOnReplace: true);

        var d1 = new TestDisposable();
        var d2 = new TestDisposable();

        bag.Item = d1;
        Assert.False(d1.Disposed);

        // Replace via dynamic member set
        bag.Item = d2;
        Assert.True(d1.Disposed);
        Assert.False(d2.Disposed);
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void DisposeOnReplace_Via_TrySetIndex_Disposes_Old_Value()
    {
        var host = new object();
        dynamic bag = host.SideData(disposeOnReplace: true);

        var d1 = new TestDisposable();
        var d2 = new TestDisposable();

        bag["key"] = d1;
        Assert.False(d1.Disposed);

        // Replace via dynamic indexer set
        bag["key"] = d2;
        Assert.True(d1.Disposed);
        Assert.False(d2.Disposed);
    }

    #endregion


    #region ObjectDisposedException Coverage

    [Fact]
    [Trait("Category", "Disposal")]
    public void DynamicIndexer_Get_Throws_After_Dispose()
    {
        var host = new object();
        dynamic bag = host.SideData();
        bag["X"] = 1;
        ((IDisposable)bag).Dispose();

        Assert.Throws<ObjectDisposedException>(() => { var _ = bag["X"]; });
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void DynamicIndexer_Set_Throws_After_Dispose()
    {
        var host = new object();
        dynamic bag = host.SideData();
        ((IDisposable)bag).Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag["X"] = 1);
    }

    [Fact]
    [Trait("Category", "Disposal")]
    public void DynamicMember_Set_Throws_After_Dispose()
    {
        var host = new object();
        dynamic bag = host.SideData();
        ((IDisposable)bag).Dispose();

        Assert.Throws<ObjectDisposedException>(() => bag.X = 1);
    }

    #endregion


    private static void ForceFullGc()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    
    private TestDataFixture Fixture { get; } = testDataFixture;
}
