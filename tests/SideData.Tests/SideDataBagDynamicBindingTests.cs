using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using Microsoft.CSharp.RuntimeBinder;

namespace SideData;

public class SideDataBagDynamicBindingTests
{
    [Test]
    [Property("Category", "Internals")]
    public async Task Bag_Does_Not_Derive_From_DynamicObject()
    {
        // DynamicObject's constructor requires dynamic code, so deriving from it makes every Native AOT publish report IL3053 for this assembly
        await Assert.That(typeof(SideDataBag).BaseType).IsEqualTo(typeof(object));
    }


    [Test]
    public async Task Missing_Member_Throws_RuntimeBinderException()
    {
        dynamic bag = new object().SideData();

        await Assert.That(() => { var _ = bag.Missing; }).ThrowsExactly<RuntimeBinderException>();
    }


    [Test]
    public async Task Null_Value_Is_Returned_Rather_Than_Treated_As_Missing()
    {
        dynamic bag = new object().SideData();
        bag.A = null;

        await Assert.That((object?)bag.A).IsNull();
    }


    [Test]
    public async Task Real_Property_Takes_Precedence_Over_An_Entry_Of_The_Same_Name()
    {
        dynamic bag = new object().SideData(disposeOnReplace: true);
        bag.DisposeOnReplace = 5;

        await Assert.That((object)bag.DisposeOnReplace).IsEqualTo(true);
    }


    [Test]
    public async Task Real_Method_Takes_Precedence_Over_Entries()
    {
        var host = new object();
        dynamic bag = host.SideData();
        bag.A = 1;

        await Assert.That((bool)bag.Remove("A")).IsTrue();
        await Assert.That(host.SideDataBag().TryGet<int>("A", out _)).IsFalse();
    }


    [Test]
    public async Task Stored_Delegate_Can_Be_Invoked_As_A_Member()
    {
        dynamic bag = new object().SideData();
        bag.Seven = (Func<int>)(() => 7);

        int result = bag.Seven();
        await Assert.That(result).IsEqualTo(7);
    }


    [Test]
    public async Task Compound_Assignment_Reads_And_Writes_The_Entry()
    {
        dynamic bag = new object().SideData();
        bag.N = 1;
        bag.N += 2;

        await Assert.That((int)bag.N).IsEqualTo(3);
    }


    [Test]
    public async Task Member_Indexer_And_Typed_Accessors_Share_Storage()
    {
        var host = new object();
        dynamic bag = host.SideData();
        bag.A = 3;

        await Assert.That((int)bag["A"]).IsEqualTo(3);
        await Assert.That(host.SideDataBag().GetOrDefault<int>("A")).IsEqualTo(3);
    }


    [Test]
    public async Task One_Call_Site_Handles_A_Bag_Then_Another_Type_Then_A_Bag_Again()
    {
        var results = new[] {
            TryAssign(new SideDataBag()),
            TryAssign(new SideDataBag()),
            TryAssign(new object()),
            TryAssign(new SideDataBag())
        };

        await Assert.That(results).IsEquivalentTo(["assigned", "assigned", "rejected", "assigned"]);
    }


    [Test]
    public async Task Meta_Object_Lists_Current_Member_Names()
    {
        var bag = new SideDataBag();
        bag.GetOrAdd("A", 1);
        bag.GetOrAdd("B", 2);

        var meta = ((IDynamicMetaObjectProvider)bag).GetMetaObject(Expression.Constant(bag));

        await Assert.That(meta.GetDynamicMemberNames().ToArray()).IsEquivalentTo(["A", "B"]);
    }


    // C# has no syntax for deleting a member, so this goes through a call site the way other DLR languages would
    [Test]
    public async Task Delete_Member_Removes_The_Entry_And_Rejects_A_Missing_One()
    {
        var bag = new SideDataBag();
        bag.GetOrAdd("A", 1);

        await Assert.That(TryDelete(bag, "A")).IsTrue();
        await Assert.That(bag.TryGet<int>("A", out _)).IsFalse();
        await Assert.That(TryDelete(bag, "A")).IsFalse();
    }


    private static string TryAssign(dynamic target)
    {
        try {
            target.A = 1;
            return "assigned";
        } catch (RuntimeBinderException) {
            return "rejected";
        }
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


    private sealed class DeleteBinder(string name) : DeleteMemberBinder(name, ignoreCase: false)
    {
        public override DynamicMetaObject FallbackDeleteMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
            => errorSuggestion ?? new DynamicMetaObject(Expression.Throw(Expression.Constant(new MissingMemberException(Name))), target.Restrictions);
    }
}
