namespace SideData;

public static class ObjectExtensions
{
    /// <summary>
    /// <para>Provides access to methods for adding, getting and removing additional "side data" from an object. When the object is collected by
    /// the GC, any attached "side data" will also be collected as long as there are no other active references.</para>
    /// <para>One use-case for this is when writing an Extension Method for a 3rd-party class. If your extension method needs to store some kind
    /// of "state" but the extended class doesn't have an appropriate & accessible field/property to store it in, you could attach your state data
    /// to the object using SideData.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">An object that has/will-have side data attached to it.</param>
    /// <returns>An instance of the <see cref="SideData{T}"/> class.</returns>
    public static SideData<T> SideData<T>(this T obj) where T : class
        => new(obj);
}
