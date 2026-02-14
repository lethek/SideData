using SideData.Fixtures;
using SideData.LowLevel;
using SideData.TestData;

namespace SideData;

public class EdgeCaseTests : IClassFixture<TestDataFixture>
{
    public EdgeCaseTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


    [Fact]
    public void Set_NullValue_StoresAndRetrievesNull()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set<string?>(null);

        var result = user.TypedSideData().Get<string?>();
        Assert.Null(result);
    }


    [Fact]
    public void TryGet_NullValue_ReturnsTrueWithNull()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set<string?>(null);

        var success = user.TypedSideData().TryGet<string?>(out var result);
        Assert.True(success);
        Assert.Null(result);
    }


    [Fact]
    public void Get_NonExistentData_ThrowsKeyNotFoundException()
    {
        var user = Fixture.UserFaker.Generate();

        Assert.Throws<KeyNotFoundException>(() => user.TypedSideData().Get<string>());
    }


    [Fact]
    public void TryGet_NonExistentData_ReturnsFalse()
    {
        var user = Fixture.UserFaker.Generate();

        var success = user.TypedSideData().TryGet<string>(out var result);
        Assert.False(success);
        Assert.Null(result);
    }


    [Fact]
    public void GetOrDefault_NonExistentData_ReturnsDefault()
    {
        var user = Fixture.UserFaker.Generate();

        var result = user.TypedSideData().GetOrDefault<string>();
        Assert.Null(result);
    }


    [Fact]
    public void GetOrDefault_WithCustomDefault_ReturnsCustomDefault()
    {
        var user = Fixture.UserFaker.Generate();

        var result = user.TypedSideData().GetOrDefault("default-value");
        Assert.Equal("default-value", result);
    }


    [Fact]
    public void GetOrDefault_ExistingData_ReturnsStoredValue()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set("stored-value");

        var result = user.TypedSideData().GetOrDefault("default-value");
        Assert.Equal("stored-value", result);
    }


    [Fact]
    public void Add_DuplicateKey_ThrowsArgumentException()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Add("first-value");

        Assert.Throws<ArgumentException>(() => user.TypedSideData().Add("second-value"));
    }


    [Fact]
    public void TryAdd_DuplicateKey_ReturnsFalse()
    {
        var user = Fixture.UserFaker.Generate();
        var firstResult = user.TypedSideData().TryAdd("first-value");
        var secondResult = user.TypedSideData().TryAdd("second-value");

        Assert.True(firstResult);
        Assert.False(secondResult);

        // Original value should remain
        var stored = user.TypedSideData().Get<string>();
        Assert.Equal("first-value", stored);
    }


    [Fact]
    public void Remove_NonExistentData_DoesNotThrow()
    {
        var user = Fixture.UserFaker.Generate();

        // Should not throw even if nothing to remove
        user.TypedSideData().Remove<string>();
    }


    [Fact]
    public void Remove_ExistingData_RemovesSuccessfully()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set("test-value");

        user.TypedSideData().Remove<string>();

        Assert.Throws<KeyNotFoundException>(() => user.TypedSideData().Get<string>());
    }


    [Fact]
    public void SideData_ValueTypes_CanBeStored()
    {
        var user = Fixture.UserFaker.Generate();

        user.TypedSideData().Set(42);
        user.TypedSideData().Set(3.14);
        user.TypedSideData().Set(true);
        user.TypedSideData().Set(DateTime.UtcNow);

        Assert.Equal(42, user.TypedSideData().Get<int>());
        Assert.Equal(3.14, user.TypedSideData().Get<double>());
        Assert.True(user.TypedSideData().Get<bool>());
        Assert.IsType<DateTime>(user.TypedSideData().Get<DateTime>());
    }


    [Fact]
    public void SideData_DefaultValueTypes_CanBeStored()
    {
        var user = Fixture.UserFaker.Generate();

        user.TypedSideData().Set(0);
        user.TypedSideData().Set(false);
        user.TypedSideData().Set(default(DateTime));

        Assert.Equal(0, user.TypedSideData().Get<int>());
        Assert.False(user.TypedSideData().Get<bool>());
        Assert.Equal(default(DateTime), user.TypedSideData().Get<DateTime>());
    }


    [Fact]
    public void GetOrAdd_FactoryReturnsNull_StoresNull()
    {
        var user = Fixture.UserFaker.Generate();

        var result = user.TypedSideData().GetOrAdd<string?>(_ => null);
        Assert.Null(result);

        // Verify it was actually stored
        var retrieved = user.TypedSideData().Get<string?>();
        Assert.Null(retrieved);
    }


    [Fact]
    public void GetOrAdd_FactoryThrows_ExceptionPropagates()
    {
        var user = Fixture.UserFaker.Generate();

        Assert.Throws<InvalidOperationException>(() =>
            user.TypedSideData().GetOrAdd<string>(_ => throw new InvalidOperationException("Test exception"))
        );
    }


    [Fact]
    public void SideData_ComplexTypes_PreserveIdentity()
    {
        var user = Fixture.UserFaker.Generate();
        var complexObject = new { Name = "Test", Values = new[] { 1, 2, 3 }, Nested = new { Prop = "Value" } };

        // Use the actual anonymous type for storage and retrieval
        var anonymousType = complexObject.GetType();
        user.TypedSideData().Set(complexObject);

        // Retrieve using TryGet with dynamic to avoid type issues
        dynamic retrieved = user.SideData();

        // Alternatively, test with a known complex type
        var personalData = Fixture.PersonalDataFaker.Generate();
        user.TypedSideData().Set(personalData);
        var retrievedPersonalData = user.TypedSideData().Get<PersonalData>();

        // Should be the exact same instance
        Assert.Same(personalData, retrievedPersonalData);
    }


    [Fact]
    public void DynamicSideData_NonExistentProperty_ReturnsNull()
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
    public void DynamicSideData_SetNullProperty_StoresNull()
    {
        var user = Fixture.UserFaker.Generate();
        dynamic sideData = user.SideData();

        var expando = (IDictionary<string, object?>)sideData;
        expando["NullProp"] = null;

        Assert.True(expando.ContainsKey("NullProp"));
        Assert.Null(expando["NullProp"]);
    }


    [Fact]
    public void MultipleObjects_IndependentSideData_NoInterference()
    {
        var user1 = Fixture.UserFaker.Generate();
        var user2 = Fixture.UserFaker.Generate();
        var user3 = Fixture.UserFaker.Generate();

        user1.TypedSideData().Set("user1-data");
        user2.TypedSideData().Set("user2-data");
        user3.TypedSideData().Set("user3-data");

        Assert.Equal("user1-data", user1.TypedSideData().Get<string>());
        Assert.Equal("user2-data", user2.TypedSideData().Get<string>());
        Assert.Equal("user3-data", user3.TypedSideData().Get<string>());

        // Removing from one should not affect others
        user2.TypedSideData().Remove<string>();

        Assert.Equal("user1-data", user1.TypedSideData().Get<string>());
        Assert.Throws<KeyNotFoundException>(() => user2.TypedSideData().Get<string>());
        Assert.Equal("user3-data", user3.TypedSideData().Get<string>());
    }


    [Fact]
    public void SideData_AfterSetToNull_CanSetToNonNull()
    {
        var user = Fixture.UserFaker.Generate();

        user.TypedSideData().Set<string?>(null);
        Assert.Null(user.TypedSideData().Get<string?>());

        user.TypedSideData().Set<string?>("new-value");
        Assert.Equal("new-value", user.TypedSideData().Get<string?>());
    }


    private TestDataFixture Fixture { get; }
}
