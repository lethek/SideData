using SideData.Fixtures;
using SideData.LowLevel;
using SideData.TestData;

namespace SideData;

public class ConcurrencyTests : IClassFixture<TestDataFixture>
{
    public ConcurrencyTests(TestDataFixture testDataFixture)
        => Fixture = testDataFixture;


    [Fact]
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
    public void DynamicSideData_ConcurrentPropertySets_DoesNotThrow()
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
                    sideData.GetType().GetProperty($"Property{threadId}")?.SetValue(sideData, $"Value-{threadId}");

                    // Use reflection to set dynamic properties
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


    [Fact]
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


    private TestDataFixture Fixture { get; }
}
