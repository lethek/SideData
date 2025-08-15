using System.Collections.Concurrent;
using SideData.TestData;

namespace SideData;

public class SideDataAttachmentTests
{
    [Fact]
    public void GetOrAdd_Creates_And_Reuses_Bag_For_Same_Host()
    {
        var host = new object();
        var first = SideDataAttachment.GetOrAdd(host);
        var second = SideDataAttachment.GetOrAdd(host);
        Assert.Same(first, second);
    }

    
    [Fact]
    public void Different_Hosts_Get_Different_Bags()
    {
        var host1 = new object();
        var host2 = new object();

        var bag1 = SideDataAttachment.GetOrAdd(host1);
        var bag2 = SideDataAttachment.GetOrAdd(host2);

        Assert.NotSame(bag1, bag2);
    }

    
    [Fact]
    public void TryGet_Returns_False_When_Absent_True_When_Present()
    {
        var host = new object();
        Assert.False(SideDataAttachment.TryGet(host, out dynamic _));

        var created = SideDataAttachment.GetOrAdd(host);
        Assert.True(SideDataAttachment.TryGet(host, out dynamic found));
        Assert.Same(created, found);
    }

    
    [Fact]
    public void Get_Throws_When_Absent_Then_Returns_When_Present()
    {
        var host = new object();
        Assert.Throws<KeyNotFoundException>(() => SideDataAttachment.Get(host));

        var created = SideDataAttachment.GetOrAdd(host);
        var got = SideDataAttachment.Get(host);
        Assert.Same(created, got);
    }

    
    [Fact]
    public void Remove_Detaches_And_Disposes_Bag()
    {
        var host = new object();
        dynamic bag = SideDataAttachment.GetOrAdd(host);

        //Put a disposable inside the bag to verify disposal via SideDataAttachment.Remove
        var d = new TestDisposable();
        bag.D = d;

        //Remove should dispose the bag (which disposes tracked members) and detach it
        SideDataAttachment.Remove(host);

        Assert.True(d.Disposed);
        Assert.False(SideDataAttachment.TryGet(host, out dynamic _));
        Assert.Throws<KeyNotFoundException>(() => SideDataAttachment.Get(host));
    }
    

    [Fact]
    public void Remove_On_Host_With_No_Bag_Is_NoOp()
    {
        var host = new object();
        //Should not throw
        SideDataAttachment.Remove(host);
        //Subsequent calls are still no-op
        SideDataAttachment.Remove(host);
    }

    
    [Fact]
    public void GetOrAdd_Uses_Factory_Result()
    {
        var host = new object();
        var custom = new SideDataBag();
        var created = SideDataAttachment.GetOrAdd(host, () => (dynamic)custom);
        Assert.Same(custom, created);

        //Ensure it is cached for the host
        var again = SideDataAttachment.GetOrAdd(host);
        Assert.Same(custom, again);
    }

    
    [Fact]
    public void Parallel_GetOrAdd_Is_Idempotent_Per_Host()
    {
        var host = new object();
        var set = new ConcurrentBag<object>();

        Parallel.For(0, 32, _ => {
            var bag = (object)SideDataAttachment.GetOrAdd(host);
            set.Add(bag);
        });

        //All items should refer to the same single instance
        var first = set.First();
        Assert.All(set, x => Assert.Same(first, x));
    }
}
