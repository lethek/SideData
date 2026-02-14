using SideData.Fixtures;
using SideData.LowLevel;
using SideData.TestData;

namespace SideData;

public class TypedSideDataTests : IClassFixture<TestDataFixture>
{
    public TypedSideDataTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


    [Fact]
    public void Get_ClassSideData_ReturnsSameObjectsWhichWereSet()
    {
        var user = Fixture.UserFaker.Generate();
        var expectedPersonalData = Fixture.PersonalDataFaker.Generate();
        var expectedContactData = Fixture.ContactDataFaker.Generate();

        user.TypedSideData().Set(expectedPersonalData);
        user.TypedSideData().Set(expectedContactData);

        var actualPersonalData = user.TypedSideData().Get<PersonalData>();
        var actualContactData = user.TypedSideData().Get<ContactData>();

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

        user.TypedSideData().Set(unexpected);
        user.TypedSideData().Set(expected);

        //Verify the returned object is the exact same instance as the latest of its type which was set; the former were replaced
        var actual = user.TypedSideData().Get<PersonalData>();
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
        var firstResult = user.TypedSideData().GetOrAdd(x => expected);
        Assert.Same(expected, firstResult);

        //2nd call to GetOrAdd ignores 'unexpected' and returns 'expected' instead because 'expected' is already attached
        var secondResult = user.TypedSideData().GetOrAdd(x => unexpected);
        Assert.NotSame(unexpected, secondResult);
        Assert.Same(expected, secondResult);
    }


    [Fact]
    public void Add_NewSideData_AddsSuccessfully()
    {
        var user = Fixture.UserFaker.Generate();
        var personalData = Fixture.PersonalDataFaker.Generate();

        user.TypedSideData().Add(personalData);

        var retrieved = user.TypedSideData().Get<PersonalData>();
        Assert.Same(personalData, retrieved);
    }


    [Fact]
    public void TryAdd_NewSideData_ReturnsTrueAndAdds()
    {
        var user = Fixture.UserFaker.Generate();
        var personalData = Fixture.PersonalDataFaker.Generate();

        var result = user.TypedSideData().TryAdd(personalData);

        Assert.True(result);
        var retrieved = user.TypedSideData().Get<PersonalData>();
        Assert.Same(personalData, retrieved);
    }


    [Fact]
    public void TryGet_ExistingSideData_ReturnsTrueAndValue()
    {
        var user = Fixture.UserFaker.Generate();
        var expected = Fixture.PersonalDataFaker.Generate();
        user.TypedSideData().Set(expected);

        var success = user.TypedSideData().TryGet<PersonalData>(out var actual);

        Assert.True(success);
        Assert.Same(expected, actual);
    }


    [Fact]
    public void Remove_ExistingSideData_RemovesSuccessfully()
    {
        var user = Fixture.UserFaker.Generate();
        var personalData = Fixture.PersonalDataFaker.Generate();
        user.TypedSideData().Set(personalData);

        user.TypedSideData().Remove<PersonalData>();

        var success = user.TypedSideData().TryGet<PersonalData>(out _);
        Assert.False(success);
    }


    [Fact]
    public void Set_MultipleDifferentTypes_AllStoredIndependently()
    {
        var user = Fixture.UserFaker.Generate();
        var personalData = Fixture.PersonalDataFaker.Generate();
        var contactData = Fixture.ContactDataFaker.Generate();
        var stringData = "test-string";
        var intData = 42;

        user.TypedSideData().Set(personalData);
        user.TypedSideData().Set(contactData);
        user.TypedSideData().Set(stringData);
        user.TypedSideData().Set(intData);

        Assert.Same(personalData, user.TypedSideData().Get<PersonalData>());
        Assert.Same(contactData, user.TypedSideData().Get<ContactData>());
        Assert.Equal(stringData, user.TypedSideData().Get<string>());
        Assert.Equal(intData, user.TypedSideData().Get<int>());
    }


    [Fact]
    public void GetOrAdd_FactoryReceivesHostObject()
    {
        var user = Fixture.UserFaker.Generate();
        User? capturedUser = null;

        var result = user.TypedSideData().GetOrAdd(u => {
            capturedUser = u;
            return Fixture.PersonalDataFaker.Generate();
        });

        Assert.Same(user, capturedUser);
        Assert.NotNull(result);
    }


    private TestDataFixture Fixture { get; }
}
