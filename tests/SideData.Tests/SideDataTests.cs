using SideData.Fixtures;

namespace SideData;

public class SideDataTests : IClassFixture<TestDataFixture>
{
    public SideDataTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


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


    private TestDataFixture Fixture { get; }
}
