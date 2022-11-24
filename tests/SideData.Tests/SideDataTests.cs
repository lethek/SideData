using SideData.Fixtures;

namespace SideData;

public class SideDataTests : IClassFixture<TestDataFixture>
{
    public SideDataTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


    [Fact]
    public void DynamicData_UsesSideData()
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
