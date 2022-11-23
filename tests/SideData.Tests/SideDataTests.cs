using System.Runtime.ExceptionServices;
using SideData.TestData.Classes;
using SideData.TestData.Structs;

namespace SideData;

public class SideDataTests
{
    [Fact]
    public void Get_ClassSideData_ReturnsSameObjectsThatWereSet()
    {
        var user = new User { Username = "User1" };
        var setPersonalData = new TestData.Classes.PersonalData { Name = "Michael", Age = 100 };
        var setContactData = new TestData.Classes.ContactData { AddressClass = new() { State = "NSW", Country = "Australia" } };

        user.SideData().Set(setPersonalData);
        user.SideData().Set(setContactData);

        var gotPersonalData = user.SideData().Get<TestData.Classes.PersonalData>();
        var gotContactData = user.SideData().Get<TestData.Classes.ContactData>();

        //Verify the returned objects are the exact same instances that were originally set
        Assert.Same(setPersonalData, gotPersonalData);
        Assert.Same(setContactData, gotContactData);
    }


    [Fact]
    public void Get_StructSideData_ReturnsEqualStructsThatWereSet()
    {
        var user = new User { Username = "User1" };
        var setPersonalData = new TestData.Structs.PersonalData { Name = "Michael", Age = 100 };
        var setContactData = new TestData.Structs.ContactData { Address = new() { State = "NSW", Country = "Australia" } };

        user.SideData().Set(setPersonalData);
        user.SideData().Set(setContactData);

        var gotPersonalData = user.SideData().Get<TestData.Structs.PersonalData>();
        var gotContactData = user.SideData().Get<TestData.Structs.ContactData>();

        //Verify the returns structs are equal to the structs that were originally set; they won't be the same instances because structs are pass-by-copy
        Assert.Equal(setPersonalData, gotPersonalData);
        Assert.Equal(setContactData, gotContactData);
    }


    [Fact]
    public void Set_SideData_ReplacesPreviousMatchingSideDataType()
    {
        var user = new User { Username = "User1" };
        var first = new TestData.Classes.PersonalData();
        var second = new TestData.Classes.PersonalData();

        user.SideData().Set(first);
        user.SideData().Set(second);

        var retrieved = user.SideData().Get<TestData.Classes.PersonalData>();

        //Verify the returned object is the exact same instance as the last of its type that was set
        Assert.Same(second, retrieved);
    }


    private sealed class User
    {
        public string Username { get; set; }
    }
}