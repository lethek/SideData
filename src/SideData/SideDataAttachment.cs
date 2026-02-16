using System.Dynamic;
using System.Runtime.CompilerServices;

namespace SideData;

/// <summary>
/// Provides low-level APIs to attach a dynamic side-data bag (<see cref="SideDataBag"/>) to any host object,
/// retrieve it, or remove it. Internally uses <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey,TValue}"/>
/// so the association does not prevent the host object from being garbage collected.
/// </summary>
/// <remarks>
/// - Use <see cref="ObjectExtensions.SideData{T}(T, bool)"/> for a more idiomatic extension-method based API.
/// - The lifetime of the side-data bag follows the host object. When <see cref="Remove(object)"/> is called and a bag is present,
///   it will be disposed to release any tracked resources.
/// </remarks>
public static class SideDataAttachment
{
    /// <summary>
    /// Retrieves the dynamic side-data bag attached to the specified host object.
    /// </summary>
    /// <param name="hostObject">
    /// The object whose side-data bag is being requested.
    /// </param>
    /// <returns>
    /// The dynamic side-data bag that has been previously associated with <paramref name="hostObject"/>.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no side-data bag has been attached to the specified host object.
    /// </exception>
    public static dynamic Get(object hostObject)
        => AttachedData.TryGetValue(hostObject, out var bag) && bag is not null
            ? bag
            : throw new KeyNotFoundException();


    /// <summary>
    /// Gets the side-data bag attached to the specified host object, or creates and attaches one if none exists.
    /// </summary>
    /// <param name="hostObject">The object to which the side-data bag is attached.</param>
    /// <param name="newValue">
    /// An optional factory used to create the initial value when the bag does not exist yet.
    /// If omitted, a new instance of <see cref="SideDataBag"/> is created.
    /// </param>
    /// <returns>
    /// The existing or newly created dynamic side-data bag associated with <paramref name="hostObject"/>.
    /// </returns>
    public static dynamic GetOrAdd(object hostObject, Func<dynamic>? newValue = null)
        => AttachedData.GetValue(hostObject, _ => newValue?.Invoke() ?? new SideDataBag());


    /// <summary>
    /// Attempts to retrieve the side-data bag attached to the specified host object.
    /// </summary>
    /// <param name="hostObject">The object whose attached side-data bag is requested.</param>
    /// <param name="value">When this method returns, contains the attached bag if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a bag exists for <paramref name="hostObject"/>; otherwise, <c>false</c>.</returns>
    public static bool TryGet(object hostObject, out dynamic value)
    {
        if (AttachedData.TryGetValue(hostObject, out var bag)) {
            value = bag;
            return true;
        }

        value = null!;
        return false;
    }


    /// <summary>
    /// Removes the side-data bag associated with the specified host object, disposing it if present.
    /// </summary>
    /// <param name="hostObject">
    /// The object whose attached side-data bag should be removed.
    /// </param>
    public static void Remove(object hostObject)
    {
        if (AttachedData.TryGetValue(hostObject, out var bag)) {
            if (bag is IDisposable disposable) {
                disposable.Dispose();
            } else if (bag is IAsyncDisposable asyncDisposable) {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        AttachedData.Remove(hostObject);
    }


    private static readonly ConditionalWeakTable<object, IDynamicMetaObjectProvider> AttachedData = new();
}