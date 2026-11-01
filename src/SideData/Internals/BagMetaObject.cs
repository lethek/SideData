using System.Dynamic;
using System.Linq.Expressions;

namespace SideData.Internals;

/// <summary>
/// Routes dynamic binding to the Try* methods of a <see cref="SideDataBag"/> without deriving from <see cref="DynamicObject"/>,
/// whose constructor is marked as requiring dynamic code. Follows the order <see cref="DynamicObject"/> uses: a real member of
/// the bag wins, and the bag's entries are consulted only when the language binder finds none.
/// </summary>
internal sealed class BagMetaObject(Expression expression, SideDataBag bag) : DynamicMetaObject(expression, BindingRestrictions.Empty, bag)
{
    public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
    {
        var missing = binder.FallbackGetMember(this);
        var call = Expression.Invoke(Expression.Constant(GetMember), Bag, Expression.Constant(binder));
        return binder.FallbackGetMember(this, OrFallback(call, missing, TypeRestriction));
    }


    public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
    {
        var missing = binder.FallbackSetMember(this, value);
        var call = Expression.Invoke(Expression.Constant(SetMember), Bag, Expression.Constant(binder), AsObject(value));
        return binder.FallbackSetMember(this, value, OrFallback(call, missing, TypeRestriction.Merge(value.Restrictions)));
    }


    public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
    {
        var missing = binder.FallbackDeleteMember(this);
        var body = Expression.Condition(
            Expression.Invoke(Expression.Constant(DeleteMember), Bag, Expression.Constant(binder)),
            Expression.Empty(),
            Expression.Block(typeof(void), missing.Expression),
            typeof(void));
        return binder.FallbackDeleteMember(this, new DynamicMetaObject(body, TypeRestriction.Merge(missing.Restrictions)));
    }


    // Lets a stored delegate be called as bag.Name(args): a real method wins, otherwise the entry is fetched and invoked
    public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
    {
        var missing = binder.FallbackInvokeMember(this, args);
        var entry = Expression.Variable(typeof(object));
        var invoked = binder.FallbackInvoke(new DynamicMetaObject(entry, BindingRestrictions.Empty), args, null);
        var body = Expression.Block(
            typeof(object),
            [entry],
            Expression.Assign(entry, Expression.Invoke(Expression.Constant(GetMemberByName), Bag, Expression.Constant(binder.Name))),
            Expression.Condition(
                Expression.ReferenceEqual(entry, Expression.Constant(NotFound)),
                Expression.Convert(missing.Expression, typeof(object)),
                Expression.Convert(invoked.Expression, typeof(object))));
        var restrictions = TypeRestriction.Merge(BindingRestrictions.Combine(args)).Merge(missing.Restrictions).Merge(invoked.Restrictions);
        return binder.FallbackInvokeMember(this, args, new DynamicMetaObject(body, restrictions));
    }


    public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
    {
        var missing = binder.FallbackGetIndex(this, indexes);
        var call = Expression.Invoke(Expression.Constant(GetIndex), Bag, Expression.Constant(binder), AsArray(indexes));
        return binder.FallbackGetIndex(this, indexes, OrFallback(call, missing, TypeRestriction.Merge(BindingRestrictions.Combine(indexes))));
    }


    public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
    {
        var missing = binder.FallbackSetIndex(this, indexes, value);
        var call = Expression.Invoke(Expression.Constant(SetIndex), Bag, Expression.Constant(binder), AsArray(indexes), AsObject(value));
        var restrictions = TypeRestriction.Merge(BindingRestrictions.Combine(indexes)).Merge(value.Restrictions);
        return binder.FallbackSetIndex(this, indexes, value, OrFallback(call, missing, restrictions));
    }


    public override IEnumerable<string> GetDynamicMemberNames()
        => bag.GetDynamicMemberNames();


    private Expression Bag => Expression.Convert(Expression, typeof(SideDataBag));


    private BindingRestrictions TypeRestriction => BindingRestrictions.GetTypeRestriction(Expression, typeof(SideDataBag));


    private static Expression AsObject(DynamicMetaObject arg)
        => Expression.Convert(arg.Expression, typeof(object));


#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The element type is always object, so the array type is known at compile time.")]
#endif
    private static Expression AsArray(DynamicMetaObject[] args)
        => Expression.NewArrayInit(typeof(object), args.Select(AsObject));


    // The Try* methods report a miss with NotFound, because null is a legitimate stored value
    private static DynamicMetaObject OrFallback(Expression call, DynamicMetaObject missing, BindingRestrictions restrictions)
    {
        var result = Expression.Variable(typeof(object));
        var body = Expression.Block(
            typeof(object),
            [result],
            Expression.Assign(result, call),
            Expression.Condition(
                Expression.ReferenceEqual(result, Expression.Constant(NotFound)),
                Expression.Convert(missing.Expression, typeof(object)),
                result));
        return new DynamicMetaObject(body, restrictions.Merge(missing.Restrictions));
    }


    private static readonly object NotFound = new();

    private static readonly Func<SideDataBag, GetMemberBinder, object?> GetMember
        = (b, binder) => b.TryGetMember(binder, out var result) ? result : NotFound;

    private static readonly Func<SideDataBag, string, object?> GetMemberByName
        = (b, name) => b.TryGetMember(name, out var result) ? result : NotFound;

    private static readonly Func<SideDataBag, SetMemberBinder, object?, object?> SetMember
        = (b, binder, value) => b.TrySetMember(binder, value) ? value : NotFound;

    private static readonly Func<SideDataBag, DeleteMemberBinder, bool> DeleteMember
        = (b, binder) => b.TryDeleteMember(binder);

    private static readonly Func<SideDataBag, GetIndexBinder, object[], object?> GetIndex
        = (b, binder, indexes) => b.TryGetIndex(binder, indexes, out var result) ? result : NotFound;

    private static readonly Func<SideDataBag, SetIndexBinder, object[], object?, object?> SetIndex
        = (b, binder, indexes, value) => b.TrySetIndex(binder, indexes, value) ? value : NotFound;
}
