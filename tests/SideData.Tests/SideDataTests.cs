using SideData.Fixtures;

namespace SideData;

public class SideDataTests(TestDataFixture testDataFixture) : IClassFixture<TestDataFixture>
{
    [Fact]
    public void SideData_ForEachHostObject_IsUniquePerHostObject()
    {
        var user1 = Fixture.UserFaker.Generate();
        var user1FirstSideData = user1.SideData();
        var user1SecondSideData = user1.SideData();
        Assert.Same(user1FirstSideData, user1SecondSideData);

        var user2 = Fixture.UserFaker.Generate();
        var user2FirstSideData = user2.SideData();
        var user2SecondSideData = user2.SideData();
        Assert.Same(user2FirstSideData, user2SecondSideData);

        //SideData for user1 should be different to the SideData for user2
        Assert.NotSame(user1FirstSideData, user2FirstSideData);
    }


    [Fact]
    public void SetAndGet_MultipleSideData_ReturnsSameValues()
    {
        var user = Fixture.UserFaker.Generate();
        var primaryContact = Fixture.ContactDataFaker.Generate();
        var secondaryContact = Fixture.ContactDataFaker.Generate();

        user.SideData().PrimaryContact = primaryContact;
        user.SideData().SecondaryContact = secondaryContact;

        Assert.NotSame(primaryContact, secondaryContact);
        Assert.Same(primaryContact, user.SideData().PrimaryContact);
        Assert.Same(secondaryContact, user.SideData().SecondaryContact);
    }


    [Fact]
    public void SideData_OverwriteProperty_NewValueStored()
    {
        var user = Fixture.UserFaker.Generate();
        var firstContact = Fixture.ContactDataFaker.Generate();
        var secondContact = Fixture.ContactDataFaker.Generate();

        user.SideData().Contact = firstContact;
        user.SideData().Contact = secondContact;

        Assert.Same(secondContact, user.SideData().Contact);
        Assert.NotSame(firstContact, user.SideData().Contact);
    }


    [Fact]
    public void SideData_DifferentPropertyTypes_AllStoredCorrectly()
    {
        var user = Fixture.UserFaker.Generate();

        user.SideData().StringValue = "test";
        user.SideData().IntValue = 42;
        user.SideData().BoolValue = true;
        user.SideData().DateValue = DateTime.UtcNow;
        user.SideData().ArrayValue = new[] { 1, 2, 3 };

        Assert.Equal("test", user.SideData().StringValue);
        Assert.Equal(42, user.SideData().IntValue);
        Assert.True(user.SideData().BoolValue);
        Assert.IsType<DateTime>(user.SideData().DateValue);
        Assert.Equal(new[] { 1, 2, 3 }, user.SideData().ArrayValue);
    }


    [Fact]
    public void SideData_CanStoreNullValues()
    {
        var user = Fixture.UserFaker.Generate();

        user.SideData().NullValue = null;

        Assert.Null(user.SideData().NullValue);
    }


    [Fact]
    public void SideData_UsedAsIDictionary_WorksCorrectly()
    {
        var user = Fixture.UserFaker.Generate();
        dynamic sideData = user.SideData();
        var dict = (IDictionary<string, object?>)sideData;

        dict["Key1"] = "Value1";
        dict["Key2"] = 42;

        Assert.Equal("Value1", dict["Key1"]);
        Assert.Equal(42, dict["Key2"]);
        Assert.Equal(2, dict.Count);
        Assert.True(dict.ContainsKey("Key1"));
        Assert.False(dict.ContainsKey("NonExistent"));
    }


    [Fact]
    public void SideData_RemoveProperty_PropertyNoLongerAccessible()
    {
        var user = Fixture.UserFaker.Generate();
        dynamic sideData = user.SideData();
        var dict = (IDictionary<string, object?>)sideData;

        dict["TempProperty"] = "temp-value";
        Assert.True(dict.ContainsKey("TempProperty"));

        dict.Remove("TempProperty");
        Assert.False(dict.ContainsKey("TempProperty"));
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void SideData_NonExistentProperty_ReturnsNull()
    {
        var user = Fixture.UserFaker.Generate();
        dynamic sideData = user.SideData();

        // Accessing non-existent dynamic property should return null (ExpandoObject behavior)
        var expando = (IDictionary<string, object?>)sideData;
        var hasProperty = expando.TryGetValue("NonExistent", out var value);

        Assert.False(hasProperty);
        Assert.Null(value);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void SideData_SetNullProperty_StoresNull()
    {
        var user = Fixture.UserFaker.Generate();
        dynamic sideData = user.SideData();

        var expando = (IDictionary<string, object?>)sideData;
        expando["NullProp"] = null;

        Assert.True(expando.ContainsKey("NullProp"));
        Assert.Null(expando["NullProp"]);
    }


    [Fact]
    [Trait("Category", "Memory")]
    public void SideData_WhenHostObjectCollected_ExpandoObjectIsCollectable()
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
    [Trait("Category", "Concurrency")]
    public void SideData_ConcurrentPropertySets_DoesNotThrow()
    {
        var user = Fixture.UserFaker.Generate();
        var threads = new Thread[10];
        var exceptions = new List<Exception>();
        var lockObj = new object();

        for (int i = 0; i < threads.Length; i++) {
            var threadId = i;
            threads[i] = new Thread(() => {
                try {
                    dynamic sideData = user.SideData();
                    // Each thread sets different properties
                    var expando = (IDictionary<string, object?>)sideData;
                    expando[$"Prop{threadId}"] = threadId;
                } catch (Exception ex) {
                    lock (lockObj) {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        foreach (var thread in threads) {
            thread.Start();
        }

        foreach (var thread in threads) {
            thread.Join();
        }

        // Should not throw during concurrent access
        Assert.Empty(exceptions);
    }


    private TestDataFixture Fixture { get; } = testDataFixture;
}
