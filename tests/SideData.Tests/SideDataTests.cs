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


    private TestDataFixture Fixture { get; }
}
