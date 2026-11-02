using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace SideData;

// The C# binder has no syntax for deleting a member and routes string keys to the real indexer,
// so these paths are only reachable through a call site the way another DLR language would use it
public class SideDataBagDynamicBindingTests
{
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
}
