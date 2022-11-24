using Bogus;

using SideData.TestData;


namespace SideData.Fixtures;

public class TestDataFixture
{
    internal readonly Faker<User> UserFaker = new Faker<User>()
        .StrictMode(true)
        .RuleFor(x => x.Username, x => x.Internet.UserName());


    internal readonly Faker<PersonalData> PersonalDataFaker = new Faker<PersonalData>()
        .StrictMode(true)
        .RuleFor(x => x.Name, x => x.Name.FullName())
        .RuleFor(x => x.DateOfBirth, x => x.Person.DateOfBirth);


    internal readonly Faker<ContactData> ContactDataFaker = new Faker<ContactData>()
        .StrictMode(true)
        .RuleFor(x => x.Address, x => x.Address.FullAddress())
        .RuleFor(x => x.Phone, x => x.Phone.PhoneNumber());
}
