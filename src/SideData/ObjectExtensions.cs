namespace SideData;

public static class ObjectExtensions
{
    /// <summary>
    /// Retrieves a dynamic side-data container attached to the specified host object.
    /// The returned value can be used to store or retrieve arbitrary properties at runtime.
    /// </summary>
    /// <typeparam name="T">The type of the host object that will receive the side data.</typeparam>
    /// <param name="obj">The instance to which the side data will be attached.</param>
    /// <param name="disposeOnReplace">
    /// If <c>true</c>, any existing value that is replaced by a new assignment will be disposed when the replacement occurs,
    /// ensuring that disposable side-data does not leak resources.
    /// </param>
    /// <returns>
    /// A dynamic object representing the side-data bag associated with <paramref name="obj"/>.
    /// The returned instance can be indexed or accessed via dynamic member syntax to store and retrieve arbitrary data.
    /// If no side-data bag exists for <paramref name="obj"/>, a new one is created and stored automatically.
    /// </returns>
    public static dynamic SideData<T>(this T obj, bool disposeOnReplace = false) where T : class
        => SideDataAttachment.GetOrAdd(obj, () => new SideDataBag(disposeOnReplace));


    /// <summary>
    /// Represents a dynamic bag that can be attached to any object via <see cref="ObjectExtensions.SideData{T}(T, bool)"/>.
    /// The bag allows storage of arbitrary properties at runtime using dynamic member access or indexer syntax.
    /// </summary>
    /// <param name="disposeOnReplace">
    /// If <c>true</c>, any existing value that is replaced by a new assignment will be disposed when the replacement occurs.
    /// This ensures that disposable side-data does not leak resources.
    /// </param>
    public static SideDataBag SideDataBag<T>(this T obj, bool disposeOnReplace = false) where T : class
        => SideDataAttachment.GetOrAdd(obj, () => new SideDataBag(disposeOnReplace));
}
