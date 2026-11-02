using System.Diagnostics.CodeAnalysis;

namespace SideData;

public class ObjectExtensionsTests
{
    [Test]
    [Property("Category", "Internals")]
    public async Task Only_The_Dynamic_Entry_Point_Requires_Dynamic_Code()
    {
        // Native AOT apps using the typed API must see no warning, and apps using the dynamic API must see one
        var dynamicEntry = typeof(ObjectExtensions).GetMethod(nameof(ObjectExtensions.SideData))!;
        var typedEntry = typeof(ObjectExtensions).GetMethod(nameof(ObjectExtensions.SideDataBag))!;

        await Assert.That(dynamicEntry.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false)).IsTrue();
        await Assert.That(typedEntry.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false)).IsFalse();
    }
}
