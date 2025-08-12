using System;
using System.Collections;
using CleanEventBus.Application;
using CleanEventBus.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CleanEventBus.Tests.Runtime
{
    public class EventBusStressTests
    {
        private ApplicationEventBus _eventBus;
        
        public class StressTestEvent : ApplicationEvent
        {
            public int Id { get; set; }
            public string Data { get; set; }
        }
        
        [TargetedEvent("TargetId")]
        public class StressTestTargetedEvent : ApplicationEvent
        {
            public string TargetId { get; set; }
            public byte[] Payload { get; set; }
        }
        
        [SetUp]
        public void Setup()
        {
            _eventBus = new ApplicationEventBus();
        }
        
        [Test]
        [Category("Performance")]
        public void Should_Handle_10000_Subscribers_Without_Issues()
        {
            const int subscriberCount = 10000;
            int callbacksExecuted = 0;
            
            // Subscribe 10,000 callbacks
            for (int i = 0; i < subscriberCount; i++)
            {
                _eventBus.Subscribe<StressTestEvent>(_ => 
                {
                    System.Threading.Interlocked.Increment(ref callbacksExecuted);
                });
            }
            
            // Publish event
            _eventBus.Publish(new StressTestEvent { Id = 1, Data = "Stress test" });
            
            Assert.AreEqual(subscriberCount, callbacksExecuted);
            UnityEngine.Debug.Log($"Successfully handled {subscriberCount} subscribers");
        }
        
        [Test]
        [Category("Performance")]
        public void Should_Handle_Large_Payloads_Efficiently()
        {
            const int payloadSize = 1024 * 1024; // 1MB
            const int eventCount = 100;
    
            bool eventReceived = false;
    
            // ✅ Subscribe with targeted event context
            using (EventContext.SetContext("stress_test"))
            {
                _eventBus.Subscribe<StressTestTargetedEvent>(evt => 
                {
                    eventReceived = true;
                    Assert.AreEqual(payloadSize, evt.Payload.Length);
                });
            }
    
            var largePayload = new byte[payloadSize];
            for (int i = 0; i < payloadSize; i++)
            {
                largePayload[i] = (byte)(i % 256);
            }
    
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
            for (int i = 0; i < eventCount; i++)
            {
                _eventBus.Publish(new StressTestTargetedEvent 
                { 
                    TargetId = "stress_test",
                    Payload = largePayload 
                });
            }
    
            stopwatch.Stop();
    
            UnityEngine.Debug.Log($"Published {eventCount} events with {payloadSize} byte payloads in {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(eventReceived, "Event should have been received");
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "Large payload handling should complete within 5 seconds");
        }
        
        [UnityTest]
        [Category("Performance")]
        public IEnumerator Should_Handle_Rapid_Subscribe_Unsubscribe_Cycles()
        {
            const int cycles = 1000;
            const int subscribersPerCycle = 100;
            
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                var callbacks = new Action<StressTestEvent>[subscribersPerCycle];
                
                // Subscribe
                for (int i = 0; i < subscribersPerCycle; i++)
                {
                    int index = i;
                    callbacks[i] = evt => { var _ = evt.Id + index; };
                    _eventBus.Subscribe(callbacks[i]);
                }
                
                // Publish
                _eventBus.Publish(new StressTestEvent { Id = cycle, Data = $"Cycle {cycle}" });
                
                // Unsubscribe
                for (int i = 0; i < subscribersPerCycle; i++)
                {
                    _eventBus.Unsubscribe(callbacks[i]);
                }
                
                // Yield every 100 cycles to prevent timeout
                if (cycle % 100 == 0)
                {
                    yield return null;
                }
            }
            
            UnityEngine.Debug.Log($"Completed {cycles} subscribe/unsubscribe cycles successfully");
        }
    }
}