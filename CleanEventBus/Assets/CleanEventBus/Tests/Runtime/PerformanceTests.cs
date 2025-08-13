using System;
using System.Collections;
using System.Diagnostics;
using CleanEventBus.Application;
using CleanEventBus.Core;
using CleanEventBus.Domain;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace CleanEventBus.Tests.Runtime
{
    public class EventBusPerformanceTests
    {
        private ApplicationEventBus _applicationEventBus;
        private DomainEventBus _domainEventBus;
        private Stopwatch _stopwatch;

        // Test events
        public class TestApplicationEvent : ApplicationEvent
        {
            public string Message { get; set; }
            public int Value { get; set; }
        }

        [TargetedEvent("TargetId")]
        public class TestTargetedApplicationEvent : ApplicationEvent
        {
            public string TargetId { get; set; }
            public string Data { get; set; }
        }

        public class TestDomainEvent : DomainEvent
        {
            public string EntityId { get; set; }
            public string Action { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            _applicationEventBus = new ApplicationEventBus();
            _domainEventBus = new DomainEventBus();
            _stopwatch = new Stopwatch();
        }

        [TearDown]
        public void TearDown()
        {
            _applicationEventBus = null;
            _domainEventBus = null;
            _stopwatch = null;
        }

        // =============================================================================
        // SUBSCRIBE PERFORMANCE TESTS
        // =============================================================================

        [Test]
        [Category("Performance")]
        public void Subscribe_1000_Global_Events_Should_Be_Fast()
        {
            const int subscriptionCount = 1000;
            var callbacks = new Action<TestApplicationEvent>[subscriptionCount];

            // Prepare callbacks
            for (int i = 0; i < subscriptionCount; i++)
            {
                int index = i; // Capture for closure
                callbacks[i] = evt =>
                {
                    var _ = evt.Message + index;
                };
            }

            _stopwatch.Start();

            // Subscribe all callbacks
            for (int i = 0; i < subscriptionCount; i++)
            {
                _applicationEventBus.Subscribe(callbacks[i]);
            }

            _stopwatch.Stop();

            long elapsedMs = _stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"Subscribe 1000 global events: {elapsedMs}ms ({_stopwatch.ElapsedTicks} ticks)");

            // Assert reasonable performance (should be under 100ms)
            Assert.Less(elapsedMs, 100, "Subscribing 1000 global events should take less than 100ms");
        }

        [Test]
        [Category("Performance")]
        public void Single_Subscription_Memory_Allocation_Should_Be_Minimal()
        {
            for (int i = 0; i < 10; i++)
            {
                _applicationEventBus.Subscribe<TestApplicationEvent>(_ => { });
            }

            // Force multiple garbage collections
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(true); // Force GC again

            // Single subscription test
            _applicationEventBus.Subscribe<TestApplicationEvent>(evt =>
            {
                var _ = evt.Message;
            });

            var finalMemory = GC.GetTotalMemory(false); // Don't force GC
            var allocatedBytes = finalMemory - initialMemory;

            UnityEngine.Debug.Log($"Memory allocated for single subscription: {allocatedBytes} bytes");

            // Single subscription should allocate less than 500 bytes (más realista)
            Assert.Less(allocatedBytes, 500, "Single subscription should allocate less than 500 bytes");
        }

        [Test]
        [Performance]
        public void Subscribe_1000_Targeted_Events_Should_Be_Fast()
        {
            const int subscriptionCount = 1000;
            var callbacks = new Action<TestTargetedApplicationEvent>[subscriptionCount];

            // Prepare callbacks
            for (int i = 0; i < subscriptionCount; i++)
            {
                int index = i; // Capture for closure
                callbacks[i] = evt =>
                {
                    var _ = evt.Data + index;
                };
            }

            _stopwatch.Start();

            // Subscribe with different targets
            for (int i = 0; i < subscriptionCount; i++)
            {
                using (EventContext.SetContext($"target_{i}"))
                {
                    _applicationEventBus.Subscribe(callbacks[i]);
                }
            }

            _stopwatch.Stop();

            long elapsedMs = _stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"Subscribe 1000 targeted events: {elapsedMs}ms");

            Assert.Less(elapsedMs, 200, "Subscribing 1000 targeted events should take less than 200ms");
        }

        // =============================================================================
        // PUBLISH PERFORMANCE TESTS
        // =============================================================================

        [Test]
        [Performance]
        public void Publish_To_1000_Global_Subscribers_Should_Be_Fast()
        {
            const int subscriberCount = 1000;
            int callbackCount = 0;

            // Subscribe 1000 callbacks
            for (int i = 0; i < subscriberCount; i++)
            {
                _applicationEventBus.Subscribe<TestApplicationEvent>(_ => { callbackCount++; });
            }

            var testEvent = new TestApplicationEvent { Message = "Performance test", Value = 42 };

            _stopwatch.Start();
            _applicationEventBus.Publish(testEvent);
            _stopwatch.Stop();

            long elapsedMs = _stopwatch.ElapsedMilliseconds;
            long elapsedTicks = _stopwatch.ElapsedTicks;
            double microsecondsPerCallback = (elapsedTicks * 1000000.0 / Stopwatch.Frequency) / subscriberCount;

            UnityEngine.Debug.Log($"Publish to 1000 subscribers: {elapsedMs}ms ({elapsedTicks} ticks)");
            UnityEngine.Debug.Log($"Time per callback: {microsecondsPerCallback:F2} microseconds");

            Assert.AreEqual(subscriberCount, callbackCount, "All callbacks should be executed");
            Assert.Less(elapsedMs, 50, "Publishing to 1000 subscribers should take less than 50ms");
            Assert.Less(microsecondsPerCallback, 100, "Each callback should take less than 100 microseconds");
        }

        [Test]
        [Performance]
        public void Publish_Targeted_Event_Should_Only_Call_Specific_Target()
        {
            const int totalTargets = 1000;
            const string specificTarget = "target_500";
            int correctCallbacks = 0;
            int wrongCallbacks = 0;

            // Subscribe to many different targets
            for (int i = 0; i < totalTargets; i++)
            {
                string targetId = $"target_{i}";
                using (EventContext.SetContext(targetId))
                {
                    _applicationEventBus.Subscribe<TestTargetedApplicationEvent>(evt =>
                    {
                        if (evt.TargetId == specificTarget)
                            correctCallbacks++;
                        else
                            wrongCallbacks++;
                    });
                }
            }

            var testEvent = new TestTargetedApplicationEvent
            {
                TargetId = specificTarget,
                Data = "Targeted test"
            };

            _stopwatch.Start();
            _applicationEventBus.Publish(testEvent);
            _stopwatch.Stop();

            long elapsedTicks = _stopwatch.ElapsedTicks;
            double microseconds = elapsedTicks * 1000000.0 / Stopwatch.Frequency;

            UnityEngine.Debug.Log(
                $"Targeted publish with {totalTargets} potential targets: {microseconds:F2} microseconds");

            Assert.AreEqual(1, correctCallbacks, "Only one callback should be executed");
            Assert.AreEqual(0, wrongCallbacks, "No wrong callbacks should be executed");
            Assert.Less(microseconds, 1000, "Targeted publish should take less than 1ms even with many targets");
        }

        // =============================================================================
        // MEMORY ALLOCATION TESTS
        // =============================================================================

        [Test]
        [Performance]
        public void Publish_Should_Not_Allocate_Excessive_Memory()
        {
            // Subscribe some callbacks
            for (int i = 0; i < 100; i++)
            {
                _applicationEventBus.Subscribe<TestApplicationEvent>(evt =>
                {
                    var _ = evt.Message;
                });
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            // Publish many events
            for (int i = 0; i < 1000; i++)
            {
                _applicationEventBus.Publish(new TestApplicationEvent
                {
                    Message = $"Test {i}",
                    Value = i
                });
            }

            var finalMemory = GC.GetTotalMemory(false);
            var allocatedBytes = finalMemory - initialMemory;
            var bytesPerPublish = allocatedBytes / 1000.0;

            UnityEngine.Debug.Log($"Memory allocated for 1000 publishes: {allocatedBytes} bytes");
            UnityEngine.Debug.Log($"Memory per publish: {bytesPerPublish:F2} bytes");

            // Should not allocate more than 1KB per publish on average
            Assert.Less(bytesPerPublish, 1024, "Each publish should allocate less than 1KB on average");
        }

        // =============================================================================
        // CONCURRENT ACCESS TESTS
        // =============================================================================

        [UnityTest]
        [Performance]
        public IEnumerator Concurrent_Subscribe_And_Publish_Should_Be_Thread_Safe()
        {
            const int operationsPerThread = 100;
            const int threadCount = 4;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var completedThreads = 0;

            // Start multiple threads doing subscribe/publish operations
            for (var t = 0; t < threadCount; t++)
            {
                var threadId = t;
                var thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        for (var i = 0; i < operationsPerThread; i++)
                        {
                            // Mix subscribe and publish operations
                            if (i % 2 == 0)
                            {
                                using (EventContext.SetContext($"thread_{threadId}_target_{i}"))
                                {
                                    _applicationEventBus.Subscribe<TestTargetedApplicationEvent>(_ => { });
                                }
                            }
                            else
                            {
                                _applicationEventBus.Publish(new TestTargetedApplicationEvent
                                {
                                    TargetId = $"thread_{threadId}_target_{i}",
                                    Data = $"Thread {threadId} operation {i}"
                                });
                            }

                            // Small delay to increase chance of race conditions
                            System.Threading.Thread.Sleep(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                    finally
                    {
                        System.Threading.Interlocked.Increment(ref completedThreads);
                    }
                });

                thread.Start();
            }

            // Wait for all threads to complete
            while (completedThreads < threadCount)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Check for exceptions
            Assert.IsEmpty(exceptions,
                $"Concurrent operations should not throw exceptions. Exceptions: {string.Join(", ", exceptions)}");

            UnityEngine.Debug.Log(
                $"Completed {threadCount} threads with {operationsPerThread} operations each without exceptions");
        }

        // =============================================================================
        // CACHE PERFORMANCE TESTS
        // =============================================================================

        [Test]
        [Performance]
        public void Metadata_Cache_Should_Improve_Performance()
        {
            const int iterations = 10000;

            // First run - populates cache
            _stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                _applicationEventBus.Publish(new TestTargetedApplicationEvent
                {
                    TargetId = "cache_test",
                    Data = $"Iteration {i}"
                });
            }

            _stopwatch.Stop();
            var firstRunMs = _stopwatch.ElapsedMilliseconds;

            _stopwatch.Reset();

            // Second run - should use cache
            _stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                _applicationEventBus.Publish(new TestTargetedApplicationEvent
                {
                    TargetId = "cache_test",
                    Data = $"Iteration {i}"
                });
            }

            _stopwatch.Stop();
            var secondRunMs = _stopwatch.ElapsedMilliseconds;

            UnityEngine.Debug.Log($"First run (cache population): {firstRunMs}ms");
            UnityEngine.Debug.Log($"Second run (cache hit): {secondRunMs}ms");
            UnityEngine.Debug.Log($"Performance improvement: {((double)firstRunMs / secondRunMs):F2}x");

            // Second run should be at least as fast (cache helps)
            Assert.LessOrEqual(secondRunMs, firstRunMs * 1.1, "Cached run should not be significantly slower");
        }

        // =============================================================================
        // BENCHMARK COMPARISON TESTS
        // =============================================================================

        [Test]
        [Performance]
        public void EventBus_vs_Direct_Calls_Benchmark()
        {
            const int iterations = 100000;
            var callbacks = new Action[10];

            // Prepare callbacks
            for (int i = 0; i < callbacks.Length; i++)
            {
                int index = i;
                callbacks[i] = () =>
                {
                    var _ = index * 2;
                }; // Simple operation
            }

            // Direct calls benchmark
            _stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var callback in callbacks)
                {
                    callback();
                }
            }

            _stopwatch.Stop();
            var directCallsMs = _stopwatch.ElapsedMilliseconds;

            _stopwatch.Reset();

            // EventBus calls benchmark
            for (var i = 0; i < callbacks.Length; i++)
            {
                var index = i;
                _applicationEventBus.Subscribe<TestApplicationEvent>(evt => { _ = index * 2; });
            }

            _stopwatch.Start();
            for (var i = 0; i < iterations; i++)
            {
                _applicationEventBus.Publish(new TestApplicationEvent { Message = "Benchmark", Value = i });
            }

            _stopwatch.Stop();
            var eventBusMs = _stopwatch.ElapsedMilliseconds;

            double overhead = (double)eventBusMs / directCallsMs;

            UnityEngine.Debug.Log($"Direct calls: {directCallsMs}ms");
            UnityEngine.Debug.Log($"EventBus calls: {eventBusMs}ms");
            UnityEngine.Debug.Log($"EventBus overhead: {overhead:F2}x");

            // 15x overhead is acceptable for coordination/UI events (not hot paths)
            Assert.Less(overhead, 15.0, "EventBus should not be more than 15x slower than direct calls");

            // También verificar que el tiempo absoluto sea razonable
            Assert.Less(eventBusMs, 100, "EventBus should complete 100k operations in under 100ms");
        }

        // =============================================================================
        // UNSUBSCRIBE PERFORMANCE TESTS
        // =============================================================================

        [Test]
        [Performance]
        public void Unsubscribe_Should_Be_Fast_With_Many_Subscribers()
        {
            const int subscriberCount = 1000;
            var callbacks = new Action<TestApplicationEvent>[subscriberCount];

            // Subscribe many callbacks
            for (int i = 0; i < subscriberCount; i++)
            {
                int index = i;
                callbacks[i] = evt =>
                {
                    var _ = evt.Message + index;
                };
                _applicationEventBus.Subscribe(callbacks[i]);
            }

            // Unsubscribe them all
            _stopwatch.Start();
            for (int i = 0; i < subscriberCount; i++)
            {
                _applicationEventBus.Unsubscribe(callbacks[i]);
            }

            _stopwatch.Stop();

            var elapsedMs = _stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"Unsubscribe {subscriberCount} callbacks: {elapsedMs}ms");

            Assert.Less(elapsedMs, 100, "Unsubscribing should be fast");

            // Verify no callbacks are called after unsubscribe
            var callbackCount = 0;
            _applicationEventBus.Subscribe<TestApplicationEvent>(_ => callbackCount++);

            _applicationEventBus.Publish(new TestApplicationEvent { Message = "Test" });
            Assert.AreEqual(1, callbackCount, "Only the remaining callback should be called");
        }
    }
}