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
    /// <exception cref="InvalidOperationException">
    /// Thrown when a side-data bag already exists for <paramref name="obj"/> but with a different <paramref name="disposeOnReplace"/> setting.
    /// </exception>
    public static dynamic SideData<T>(this T obj, bool disposeOnReplace = false) where T : class
    {
        var bag = SideDataAttachment.GetOrAdd(obj, () => new SideDataBag(disposeOnReplace));
        if (bag is SideDataBag sdBag && sdBag.DisposeOnReplace != disposeOnReplace) {
            throw new InvalidOperationException(
                $"A side-data bag already exists for this object with disposeOnReplace={sdBag.DisposeOnReplace}. " +
                $"Cannot retrieve it with disposeOnReplace={disposeOnReplace}.");
        }
        return bag;
    }


    /// <summary>
    /// Represents a dynamic bag that can be attached to any object via <see cref="ObjectExtensions.SideData{T}(T, bool)"/>.
    /// The bag allows storage of arbitrary properties at runtime using dynamic member access or indexer syntax.
    /// </summary>
    /// <param name="disposeOnReplace">
    /// If <c>true</c>, any existing value that is replaced by a new assignment will be disposed when the replacement occurs.
    /// This ensures that disposable side-data does not leak resources.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a side-data bag already exists for <paramref name="obj"/> but with a different <paramref name="disposeOnReplace"/> setting.
    /// </exception>
    public static SideDataBag SideDataBag<T>(this T obj, bool disposeOnReplace = false) where T : class
    {
        var bag = SideDataAttachment.GetOrAdd(obj, () => new SideDataBag(disposeOnReplace));
        if (bag is SideDataBag sdBag && sdBag.DisposeOnReplace != disposeOnReplace) {
            throw new InvalidOperationException(
                $"A side-data bag already exists for this object with disposeOnReplace={sdBag.DisposeOnReplace}. " +
                $"Cannot retrieve it with disposeOnReplace={disposeOnReplace}.");
        }
        return bag;
    }
}
