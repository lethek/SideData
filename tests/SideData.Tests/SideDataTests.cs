using SideData.Fixtures;
using SideData.TestData;

namespace SideData;

public class SideDataTests : IClassFixture<TestDataFixture>
{
    public SideDataTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


    [Fact]
    public void Get_ClassSideData_ReturnsSameObjectsWhichWereSet()
    {
        var user = Fixture.UserFaker.Generate();
        var expectedPersonalData = Fixture.PersonalDataFaker.Generate();
        var expectedContactData = Fixture.ContactDataFaker.Generate();

        user.SideData().Set(expectedPersonalData);
        user.SideData().Set(expectedContactData);

        var actualPersonalData = user.SideData().Get<PersonalData>();
        var actualContactData = user.SideData().Get<ContactData>();

        //Verify the returned objects are the exact same instances that were originally set
        Assert.Same(expectedPersonalData, actualPersonalData);
        Assert.Same(expectedContactData, actualContactData);
    }


    [Fact]
    public void Set_SideData_ReplacesPreviousMatchingSideDataType()
    {
        var user = Fixture.UserFaker.Generate();
        var expected = Fixture.PersonalDataFaker.Generate();
        var unexpected = Fixture.PersonalDataFaker.Generate();

        user.SideData().Set(unexpected);
        user.SideData().Set(expected);

        //Verify the returned object is the exact same instance as the latest of its type which was set; the former were replaced
        var actual = user.SideData().Get<PersonalData>();
        Assert.NotSame(unexpected, actual);
        Assert.Same(expected, actual);
    }


    [Fact]
    public void GetOrAdd_SideData_GetsExistingDataOrIfMissingAddsAndGetsNewData()
    {
        var user = Fixture.UserFaker.Generate();
        var expected = Fixture.PersonalDataFaker.Generate();
        var unexpected = Fixture.PersonalDataFaker.Generate();

        //1st call to GetOrAdd attaches 'expected' to the object because there was nothing of the type already there
        var firstResult = user.SideData().GetOrAdd(x => expected);
        Assert.Same(expected, firstResult);

        //2nd call to GetOrAdd ignores 'unexpected' and returns 'expected' instead because 'expected' is already attached
        var secondResult = user.SideData().GetOrAdd(x => unexpected);
        Assert.NotSame(unexpected, secondResult);
        Assert.Same(expected, secondResult);
    }


    private TestDataFixture Fixture { get; }
}
