using System.Dynamic;

namespace SideData.LowLevel;

public static class ObjectExtensions
{
    /// <summary>
    /// <para>This is intended as a lower-level tool for managing an object's "side-data". If in doubt, you're probably better
    /// off using the <see cref="SideData.ObjectExtensions.SideData{T}"/> extension method instead, which returns a dynamic
    /// <see cref="ExpandoObject"/> that you can rely on.</para>
    /// 
    /// <para><see cref="TypedSideData{T}"/> provides access to methods for adding, getting and removing side-data from an
    /// object. It allows for per-type side-data only; E.g. so while you could add a String and an Int32 and an instance of your
    /// own custom Foo class, you cannot add a 2nd String or value/instance of another type which is already attached to the
    /// object. When the object is collected by the GC, any attached side-data will also be collected as long as there are no
    /// other active references.</para>
    /// 
    /// <para>One use-case for side-data is when writing an Extension Method for a 3rd-party class. If your extension method
    /// needs to store some kind of "state" but the extended class doesn't have an appropriate &amp; accessible field/property
    /// to store it in, you could attach your state data to the object with <see cref="SideData.ObjectExtensions.SideData{T}"/>
    /// or <see cref="TypedSideData{T}"/>.</para>
    /// 
    /// <para>Warning: <see cref="SideData{T}"/> has no support for automatically disposing attached <see cref="IDisposable"/>
    /// side-data, so exercise caution and make sure to either handle disposing those yourself, or avoid attaching them in the
    /// first place.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">An object that has/will-have side-data attached to it.</param>
    /// <returns>An instance of the <see cref="TypedSideData{T}"/> class for managing per-type side-data on <paramref name="obj"/>.</returns>
    public static TypedSideData<T> TypedSideData<T>(this T obj) where T : class
        => new(obj);
}
