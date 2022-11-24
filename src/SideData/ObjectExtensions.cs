using System.Dynamic;

using SideData.LowLevel;


namespace SideData;

public static class ObjectExtensions
{
    /// <summary>
    /// <para>One use-case for side-data is when writing an Extension Method for a 3rd-party class. If your extension method
    /// needs to store some kind of "state" but the extended class doesn't have an appropriate &amp; accessible field/property
    /// to store it in, you could attach your state data to the object as a dynamic field on <see cref="SideData{T}"/>.</para>
    ///
    /// <code>
    /// var obj = new { Name = "Some unchangable object" };
    /// obj.SideData().Shape = "Rectangle";
    /// obj.SideData().NumberOfEdges = 4;
    /// obj.SideData().EdgeLengths = new int[] { 6, 4, 6, 4 };
    /// </code>
    /// 
    /// <para>When the object is collected by the GC, any attached side-data will also be collected as long as that data has no
    /// other active references.</para>
    /// 
    /// <para>Warning: <see cref="SideData{T}"/> has no support for automatically disposing attached <see cref="IDisposable"/>
    /// side-data, so exercise caution and make sure to either handle disposing those yourself, or avoid attaching them in the
    /// first place.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">An object that has/will-have side-data attached to it.</param>
    /// <returns>A dynamic <see cref="ExpandoObject"/> for storing side-data on <paramref name="obj"/>.</returns>
    public static dynamic SideData<T>(this T obj) where T : class
        => obj.TypedSideData().GetOrAdd(x => new ExpandoObject());
}
