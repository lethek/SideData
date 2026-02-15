using SideData.Fixtures;
using SideData.LowLevel;
using SideData.TestData;

namespace SideData;

public class TypedSideDataTests(TestDataFixture testDataFixture) : IClassFixture<TestDataFixture>
{
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


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void Set_NullValue_StoresAndRetrievesNull()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set<string?>(null);

        var result = user.TypedSideData().Get<string?>();
        Assert.Null(result);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void TryGet_NullValue_ReturnsTrueWithNull()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set<string?>(null);

        var success = user.TypedSideData().TryGet<string?>(out var result);
        Assert.True(success);
        Assert.Null(result);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void Get_NonExistentData_ThrowsKeyNotFoundException()
    {
        var user = Fixture.UserFaker.Generate();

        Assert.Throws<KeyNotFoundException>(() => user.TypedSideData().Get<string>());
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void TryGet_NonExistentData_ReturnsFalse()
    {
        var user = Fixture.UserFaker.Generate();

        var success = user.TypedSideData().TryGet<string>(out var result);
        Assert.False(success);
        Assert.Null(result);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void GetOrDefault_NonExistentData_ReturnsDefault()
    {
        var user = Fixture.UserFaker.Generate();

        var result = user.TypedSideData().GetOrDefault<string>();
        Assert.Null(result);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void GetOrDefault_WithCustomDefault_ReturnsCustomDefault()
    {
        var user = Fixture.UserFaker.Generate();

        var result = user.TypedSideData().GetOrDefault("default-value");
        Assert.Equal("default-value", result);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void GetOrDefault_ExistingData_ReturnsStoredValue()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set("stored-value");

        var result = user.TypedSideData().GetOrDefault("default-value");
        Assert.Equal("stored-value", result);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void Add_DuplicateKey_ThrowsArgumentException()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Add("first-value");

        Assert.Throws<ArgumentException>(() => user.TypedSideData().Add("second-value"));
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
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
    [Trait("Category", "EdgeCase")]
    public void Remove_NonExistentData_DoesNotThrow()
    {
        var user = Fixture.UserFaker.Generate();

        // Should not throw even if nothing to remove
        user.TypedSideData().Remove<string>();
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void Remove_ExistingData_RemovesSuccessfully()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set("test-value");

        user.TypedSideData().Remove<string>();

        Assert.Throws<KeyNotFoundException>(() => user.TypedSideData().Get<string>());
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void TypedSideData_ValueTypes_CanBeStored()
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
    [Trait("Category", "EdgeCase")]
    public void TypedSideData_DefaultValueTypes_CanBeStored()
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
    [Trait("Category", "EdgeCase")]
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
    [Trait("Category", "EdgeCase")]
    public void GetOrAdd_FactoryThrows_ExceptionPropagates()
    {
        var user = Fixture.UserFaker.Generate();

        Assert.Throws<InvalidOperationException>(() =>
            user.TypedSideData().GetOrAdd<string>(_ => throw new InvalidOperationException("Test exception"))
        );
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
    public void TypedSideData_ComplexTypes_PreserveIdentity()
    {
        var user = Fixture.UserFaker.Generate();
        var personalData = Fixture.PersonalDataFaker.Generate();
        user.TypedSideData().Set(personalData);
        var retrievedPersonalData = user.TypedSideData().Get<PersonalData>();

        // Should be the exact same instance
        Assert.Same(personalData, retrievedPersonalData);
    }


    [Fact]
    [Trait("Category", "EdgeCase")]
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
    [Trait("Category", "EdgeCase")]
    public void TypedSideData_AfterSetToNull_CanSetToNonNull()
    {
        var user = Fixture.UserFaker.Generate();

        user.TypedSideData().Set<string?>(null);
        Assert.Null(user.TypedSideData().Get<string?>());

        user.TypedSideData().Set<string?>("new-value");
        Assert.Equal("new-value", user.TypedSideData().Get<string?>());
    }


    [Fact]
    [Trait("Category", "Memory")]
    public void TypedSideData_WhenHostObjectCollected_SideDataIsCollectable()
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
    [Trait("Category", "Memory")]
    public void TypedSideData_WhenSideDataHasExternalReference_HostCanStillBeCollected()
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
    [Trait("Category", "Memory")]
    public void TypedSideData_MultipleSideDataTypes_AllCollectedWithHost()
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
    [Trait("Category", "Memory")]
    public void TypedSideData_ManyObjectsWithSideData_NoMemoryLeaks()
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


    [Fact]
    [Trait("Category", "Concurrency")]
    public void Set_ConcurrentOperations_AllThreadsSucceed()
    {
        var user = Fixture.UserFaker.Generate();
        var threads = new Thread[10];
        var exceptions = new List<Exception>();
        var lockObj = new object();

        for (int i = 0; i < threads.Length; i++) {
            var threadId = i;
            threads[i] = new Thread(() => {
                try {
                    // Each thread sets a different type
                    user.TypedSideData().Set($"Thread-{threadId}");
                    user.TypedSideData().Set(threadId);
                    user.TypedSideData().Set(DateTime.UtcNow.AddMinutes(threadId));
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

        Assert.Empty(exceptions);
    }


    [Fact]
    [Trait("Category", "Concurrency")]
    public void Set_SameTypeConcurrentUpdates_LastWriteWins()
    {
        var user = Fixture.UserFaker.Generate();
        var threads = new Thread[20];
        var countdown = new CountdownEvent(threads.Length);

        for (int i = 0; i < threads.Length; i++) {
            var threadId = i;
            threads[i] = new Thread(() => {
                countdown.Signal();
                countdown.Wait(); // Synchronize all threads to start at same time
                user.TypedSideData().Set($"Value-{threadId}");
            });
        }

        foreach (var thread in threads) {
            thread.Start();
        }

        foreach (var thread in threads) {
            thread.Join();
        }

        // After all threads complete, there should be exactly one value
        var result = user.TypedSideData().Get<string>();
        Assert.NotNull(result);
        Assert.StartsWith("Value-", result);
    }


    [Fact]
    [Trait("Category", "Concurrency")]
    public void TryAdd_ConcurrentOperations_OnlyOneSucceeds()
    {
        var user = Fixture.UserFaker.Generate();
        var threads = new Thread[10];
        var successCount = 0;
        var lockObj = new object();

        for (int i = 0; i < threads.Length; i++) {
            var threadId = i;
            threads[i] = new Thread(() => {
                if (user.TypedSideData().TryAdd($"Thread-{threadId}")) {
                    lock (lockObj) {
                        successCount++;
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

        // Only one thread should have successfully added the value
        Assert.Equal(1, successCount);

        // And the value should be present
        var result = user.TypedSideData().Get<string>();
        Assert.NotNull(result);
        Assert.StartsWith("Thread-", result);
    }


    [Fact]
    [Trait("Category", "Concurrency")]
    public void GetOrAdd_ConcurrentCalls_AllReturnSameInstance()
    {
        var user = Fixture.UserFaker.Generate();
        var threads = new Thread[20];
        var factoryInvocations = 0;
        var results = new PersonalData?[threads.Length];

        for (int i = 0; i < threads.Length; i++) {
            var threadId = i;
            threads[i] = new Thread(() => {
                results[threadId] = user.TypedSideData().GetOrAdd(u => {
                    Interlocked.Increment(ref factoryInvocations);
                    Thread.Sleep(10); // Simulate some work
                    return Fixture.PersonalDataFaker.Generate();
                });
            });
        }

        foreach (var thread in threads) {
            thread.Start();
        }

        foreach (var thread in threads) {
            thread.Join();
        }

        // ConditionalWeakTable.GetValue may invoke factory multiple times due to race conditions
        // The important thing is all threads get the same final result
        Assert.InRange(factoryInvocations, 1, threads.Length);

        // All results should be the same instance (the one that "won" the race)
        var firstResult = results[0];
        Assert.NotNull(firstResult);
        foreach (var result in results) {
            Assert.Same(firstResult, result);
        }
    }


    [Fact]
    [Trait("Category", "Concurrency")]
    public void Remove_ConcurrentWithGet_NoExceptions()
    {
        var user = Fixture.UserFaker.Generate();
        user.TypedSideData().Set("test-value");

        var threads = new Thread[20];
        var exceptions = new List<Exception>();
        var lockObj = new object();

        for (int i = 0; i < threads.Length; i++) {
            threads[i] = new Thread(() => {
                try {
                    // Half the threads try to get, half try to remove
                    if (Thread.CurrentThread.ManagedThreadId % 2 == 0) {
                        user.TypedSideData().TryGet<string>(out _);
                    } else {
                        user.TypedSideData().Remove<string>();
                    }
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

        Assert.Empty(exceptions);
    }


    private TestDataFixture Fixture { get; } = testDataFixture;
}
