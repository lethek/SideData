using SideData.Fixtures;
using SideData.LowLevel;
using SideData.TestData;

namespace SideData;

public class MemoryTests : IClassFixture<TestDataFixture>
{
    public MemoryTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


    [Fact]
    public void SideData_WhenHostObjectCollected_SideDataIsCollectable()
    {
        WeakReference weakUser;
        WeakReference weakSideData;

        // Create scope where objects will be created and go out of scope
        void CreateAndAttachSideData()
        {
            var user = Fixture.UserFaker.Generate();
            var personalData = Fixture.PersonalDataFaker.Generate();

            weakUser = new WeakReference(user);
            weakSideData = new WeakReference(personalData);

            user.TypedSideData().Set(personalData);

            // Verify data is attached
            Assert.Same(personalData, user.TypedSideData().Get<PersonalData>());
        }

        CreateAndAttachSideData();

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Both the host object and side-data should be collected
        Assert.False(weakUser.IsAlive, "Host object should be garbage collected");
        Assert.False(weakSideData.IsAlive, "Side-data should be garbage collected");
    }


    [Fact]
    public void SideData_WhenSideDataHasExternalReference_HostCanStillBeCollected()
    {
        WeakReference weakUser;
        PersonalData? externalReference = null;

        void CreateAndAttachSideData()
        {
            var user = Fixture.UserFaker.Generate();
            var personalData = Fixture.PersonalDataFaker.Generate();

            weakUser = new WeakReference(user);
            externalReference = personalData; // Keep external reference

            user.TypedSideData().Set(personalData);
        }

        CreateAndAttachSideData();

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Host should be collected even though side-data is still referenced
        Assert.False(weakUser.IsAlive, "Host object should be garbage collected");
        Assert.NotNull(externalReference); // Side-data still alive due to external reference
    }


    [Fact]
    public void SideData_MultipleSideDataTypes_AllCollectedWithHost()
    {
        WeakReference weakUser;
        WeakReference weakPersonalData;
        WeakReference weakContactData;

        void CreateAndAttachMultipleSideData()
        {
            var user = Fixture.UserFaker.Generate();
            var personalData = Fixture.PersonalDataFaker.Generate();
            var contactData = Fixture.ContactDataFaker.Generate();

            weakUser = new WeakReference(user);
            weakPersonalData = new WeakReference(personalData);
            weakContactData = new WeakReference(contactData);

            user.TypedSideData().Set(personalData);
            user.TypedSideData().Set(contactData);

            // Verify both are attached
            Assert.Same(personalData, user.TypedSideData().Get<PersonalData>());
            Assert.Same(contactData, user.TypedSideData().Get<ContactData>());
        }

        CreateAndAttachMultipleSideData();

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // All should be collected
        Assert.False(weakUser.IsAlive);
        Assert.False(weakPersonalData.IsAlive);
        Assert.False(weakContactData.IsAlive);
    }


    [Fact]
    public void DynamicSideData_WhenHostObjectCollected_ExpandoObjectIsCollectable()
    {
        WeakReference weakUser;
        WeakReference weakExpandoObject;

        void CreateAndAttachDynamicSideData()
        {
            var user = Fixture.UserFaker.Generate();
            weakUser = new WeakReference(user);

            dynamic sideData = user.SideData();
            weakExpandoObject = new WeakReference(sideData);

            sideData.Property1 = "value1";
            sideData.Property2 = 42;

            // Verify data is accessible
            Assert.Equal("value1", sideData.Property1);
        }

        CreateAndAttachDynamicSideData();

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Both should be collected
        Assert.False(weakUser.IsAlive);
        Assert.False(weakExpandoObject.IsAlive);
    }


    [Fact]
    public void SideData_ManyObjectsWithSideData_NoMemoryLeaks()
    {
        // This test creates many objects with side-data and verifies they can all be collected
        var weakReferences = new List<WeakReference>();

        void CreateManyObjectsWithSideData()
        {
            for (int i = 0; i < 1000; i++) {
                var user = Fixture.UserFaker.Generate();
                weakReferences.Add(new WeakReference(user));

                user.TypedSideData().Set($"Data-{i}");
                user.TypedSideData().Set(i);
                user.TypedSideData().Set(Fixture.PersonalDataFaker.Generate());
                user.TypedSideData().Set(Fixture.ContactDataFaker.Generate());

                dynamic sideData = user.SideData();
                var dict = (IDictionary<string, object?>)sideData;
                dict[$"Prop{i}"] = $"Value{i}";
            }
        }

        CreateManyObjectsWithSideData();

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Count how many are still alive (should be very few or none)
        var aliveCount = weakReferences.Count(wr => wr.IsAlive);

        // Allow for some margin due to GC not being deterministic, but most should be collected
        Assert.True(aliveCount < 10, $"Too many objects still alive: {aliveCount} out of 1000");
    }


    private TestDataFixture Fixture { get; }
}
