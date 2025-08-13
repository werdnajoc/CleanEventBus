// =============================================================================
// Comprehensive Unit Tests for Subscribe/Unsubscribe Scenarios
// =============================================================================
using System;
using System.Collections.Generic;
using NUnit.Framework;
using CleanEventBus.Application;
using CleanEventBus.Core;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Tests.Runtime
{
    public class SubscriptionUnsubscriptionTests
    {
        private ApplicationEventBus eventBus;
        
        // =============================================================================
        // TEST EVENTS
        // =============================================================================
        
        [TargetedEvent("TargetId")]
        public class TargetedTestEvent : ApplicationEvent
        {
            public string TargetId { get; set; }
            public string Message { get; set; }
            public int Value { get; set; }
        }
        
        public class GlobalTestEvent : ApplicationEvent
        {
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        [TargetedEvent("CustomId")]
        public class CustomTargetEvent : ApplicationEvent
        {
            public string CustomId { get; set; }
            public string Data { get; set; }
        }
        
        [SetUp]
        public void Setup()
        {
            eventBus = new ApplicationEventBus();
        }
        
        [TearDown]
        public void TearDown()
        {
            eventBus = null;
        }
        
        // =============================================================================
        // BASIC SUBSCRIPTION TESTS
        // =============================================================================
        
        [Test]
        public void Subscribe_ShouldReturnValidToken()
        {
            // Arrange & Act
            ISubscriptionToken token;
            using (EventContext.SetContext("test_target"))
            {
                token = eventBus.Subscribe<TargetedTestEvent>(evt => { });
            }
            
            // Assert
            Assert.IsNotNull(token, "Subscribe should return a valid token");
            Assert.IsFalse(token.IsDisposed, "Token should not be disposed initially");
        }
        
        [Test]
        public void Subscribe_Global_ShouldReturnValidToken()
        {
            // Arrange & Act
            var token = eventBus.Subscribe<GlobalTestEvent>(evt => { });
            
            // Assert
            Assert.IsNotNull(token, "Global subscribe should return a valid token");
            Assert.IsFalse(token.IsDisposed, "Token should not be disposed initially");
        }
        
        [Test]
        public void Subscribe_WithNullCallback_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => {
                eventBus.Subscribe<GlobalTestEvent>(null);
            });
        }
        
        [Test]
        public void Subscribe_Targeted_EventShouldBeReceived()
        {
            // Arrange
            string targetId = "target_123";
            bool eventReceived = false;
            TargetedTestEvent receivedEvent = null;
            
            using (EventContext.SetContext(targetId))
            {
                eventBus.Subscribe<TargetedTestEvent>(evt => {
                    eventReceived = true;
                    receivedEvent = evt;
                });
            }
            
            // Act
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Test", Value = 42 });
            
            // Assert
            Assert.IsTrue(eventReceived, "Targeted event should be received");
            Assert.IsNotNull(receivedEvent, "Received event should not be null");
            Assert.AreEqual(targetId, receivedEvent.TargetId);
            Assert.AreEqual("Test", receivedEvent.Message);
            Assert.AreEqual(42, receivedEvent.Value);
        }
        
        [Test]
        public void Subscribe_Global_EventShouldBeReceived()
        {
            // Arrange
            bool eventReceived = false;
            GlobalTestEvent receivedEvent = null;
            
            eventBus.Subscribe<GlobalTestEvent>(evt => {
                eventReceived = true;
                receivedEvent = evt;
            });
            
            // Act
            var testEvent = new GlobalTestEvent { Message = "Global test", Timestamp = DateTime.UtcNow };
            eventBus.Publish(testEvent);
            
            // Assert
            Assert.IsTrue(eventReceived, "Global event should be received");
            Assert.IsNotNull(receivedEvent, "Received event should not be null");
            Assert.AreEqual("Global test", receivedEvent.Message);
        }
        
        // =============================================================================
        // MULTIPLE SUBSCRIPTION TESTS
        // =============================================================================
        
        [Test]
        public void Subscribe_MultipleCallbacks_SameTarget_AllShouldReceive()
        {
            // Arrange
            string targetId = "multi_target";
            int callback1Count = 0;
            int callback2Count = 0;
            int callback3Count = 0;
            
            using (EventContext.SetContext(targetId))
            {
                eventBus.Subscribe<TargetedTestEvent>(evt => callback1Count++);
                eventBus.Subscribe<TargetedTestEvent>(evt => callback2Count++);
                eventBus.Subscribe<TargetedTestEvent>(evt => callback3Count++);
            }
            
            // Act
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Multi test" });
            
            // Assert
            Assert.AreEqual(1, callback1Count, "Callback 1 should be called once");
            Assert.AreEqual(1, callback2Count, "Callback 2 should be called once");
            Assert.AreEqual(1, callback3Count, "Callback 3 should be called once");
        }
        
        [Test]
        public void Subscribe_MultipleCallbacks_DifferentTargets_OnlyCorrectShouldReceive()
        {
            // Arrange
            string target1 = "target_1";
            string target2 = "target_2";
            int target1Count = 0;
            int target2Count = 0;
            
            using (EventContext.SetContext(target1))
            {
                eventBus.Subscribe<TargetedTestEvent>(evt => target1Count++);
            }
            
            using (EventContext.SetContext(target2))
            {
                eventBus.Subscribe<TargetedTestEvent>(evt => target2Count++);
            }
            
            // Act
            eventBus.Publish(new TargetedTestEvent { TargetId = target1, Message = "Target 1" });
            eventBus.Publish(new TargetedTestEvent { TargetId = target2, Message = "Target 2" });
            eventBus.Publish(new TargetedTestEvent { TargetId = target1, Message = "Target 1 again" });
            
            // Assert
            Assert.AreEqual(2, target1Count, "Target 1 should receive 2 events");
            Assert.AreEqual(1, target2Count, "Target 2 should receive 1 event");
        }
        
        [Test]
        public void Subscribe_GlobalAndTargeted_BothShouldWork()
        {
            // Arrange
            string targetId = "mixed_target";
            int globalCount = 0;
            int targetedCount = 0;
            
            // Global subscription
            eventBus.Subscribe<GlobalTestEvent>(evt => globalCount++);
            
            // Targeted subscription
            using (EventContext.SetContext(targetId))
            {
                eventBus.Subscribe<TargetedTestEvent>(evt => targetedCount++);
            }
            
            // Act
            eventBus.Publish(new GlobalTestEvent { Message = "Global 1" });
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Targeted 1" });
            eventBus.Publish(new GlobalTestEvent { Message = "Global 2" });
            eventBus.Publish(new TargetedTestEvent { TargetId = "other_target", Message = "Other target" });
            
            // Assert
            Assert.AreEqual(2, globalCount, "Global events should be received");
            Assert.AreEqual(1, targetedCount, "Only correct targeted event should be received");
        }
        
        // =============================================================================
        // TOKEN DISPOSAL TESTS
        // =============================================================================
        
        [Test]
        public void TokenDispose_ShouldUnsubscribe()
        {
            // Arrange
            string targetId = "dispose_target";
            int eventCount = 0;
            ISubscriptionToken token;
            
            using (EventContext.SetContext(targetId))
            {
                token = eventBus.Subscribe<TargetedTestEvent>(evt => eventCount++);
            }
            
            // Verify subscription works
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Before dispose" });
            Assert.AreEqual(1, eventCount, "Event should be received before dispose");
            
            // Act - dispose token
            token.Dispose();
            
            // Verify unsubscription
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "After dispose" });
            Assert.AreEqual(1, eventCount, "Event should NOT be received after dispose");
            Assert.IsTrue(token.IsDisposed, "Token should be marked as disposed");
        }
        
        [Test]
        public void TokenDispose_Global_ShouldUnsubscribe()
        {
            // Arrange
            int eventCount = 0;
            var token = eventBus.Subscribe<GlobalTestEvent>(evt => eventCount++);
            
            // Verify subscription works
            eventBus.Publish(new GlobalTestEvent { Message = "Before dispose" });
            Assert.AreEqual(1, eventCount, "Event should be received before dispose");
            
            // Act - dispose token
            token.Dispose();
            
            // Verify unsubscription
            eventBus.Publish(new GlobalTestEvent { Message = "After dispose" });
            Assert.AreEqual(1, eventCount, "Event should NOT be received after dispose");
        }
        
        [Test]
        public void TokenDispose_Multiple_ShouldUnsubscribeOnlyDisposedOnes()
        {
            // Arrange
            string targetId = "multi_dispose_target";
            int callback1Count = 0;
            int callback2Count = 0;
            int callback3Count = 0;
            
            ISubscriptionToken token1, token2, token3;
            
            using (EventContext.SetContext(targetId))
            {
                token1 = eventBus.Subscribe<TargetedTestEvent>(evt => callback1Count++);
                token2 = eventBus.Subscribe<TargetedTestEvent>(evt => callback2Count++);
                token3 = eventBus.Subscribe<TargetedTestEvent>(evt => callback3Count++);
            }
            
            // Verify all work
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Before dispose" });
            Assert.AreEqual(1, callback1Count);
            Assert.AreEqual(1, callback2Count);
            Assert.AreEqual(1, callback3Count);
            
            // Act - dispose only token2
            token2.Dispose();
            
            // Verify selective unsubscription
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "After dispose" });
            Assert.AreEqual(2, callback1Count, "Callback 1 should still work");
            Assert.AreEqual(1, callback2Count, "Callback 2 should be unsubscribed");
            Assert.AreEqual(2, callback3Count, "Callback 3 should still work");
        }
        
        [Test]
        public void TokenDispose_CalledMultipleTimes_ShouldBeIdempotent()
        {
            // Arrange
            int eventCount = 0;
            var token = eventBus.Subscribe<GlobalTestEvent>(evt => eventCount++);
            
            // Act - dispose multiple times
            token.Dispose();
            token.Dispose();
            token.Dispose();
            
            // Assert - should not throw and should stay disposed
            Assert.IsTrue(token.IsDisposed, "Token should remain disposed");
            
            // Verify still unsubscribed
            eventBus.Publish(new GlobalTestEvent { Message = "After multiple disposes" });
            Assert.AreEqual(0, eventCount, "No events should be received");
        }
        
        // =============================================================================
        // MANUAL UNSUBSCRIBE TESTS (Legacy API)
        // =============================================================================
        
        [Test]
        public void Unsubscribe_Manual_ShouldWork()
        {
            // Arrange
            string targetId = "manual_unsubscribe";
            int eventCount = 0;
            Action<TargetedTestEvent> callback = evt => eventCount++;
            
            using (EventContext.SetContext(targetId))
            {
                eventBus.Subscribe<TargetedTestEvent>(callback);
            }
            
            // Verify subscription works
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Before unsubscribe" });
            Assert.AreEqual(1, eventCount, "Event should be received before unsubscribe");
            
            // Act - manual unsubscribe
            using (EventContext.SetContext(targetId))
            {
                eventBus.Unsubscribe<TargetedTestEvent>(callback);
            }
            
            // Verify unsubscription
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "After unsubscribe" });
            Assert.AreEqual(1, eventCount, "Event should NOT be received after unsubscribe");
        }
        
        [Test]
        public void Unsubscribe_Manual_Global_ShouldWork()
        {
            // Arrange
            int eventCount = 0;
            Action<GlobalTestEvent> callback = evt => eventCount++;
            
            eventBus.Subscribe<GlobalTestEvent>(callback);
            
            // Verify subscription works
            eventBus.Publish(new GlobalTestEvent { Message = "Before unsubscribe" });
            Assert.AreEqual(1, eventCount, "Event should be received before unsubscribe");
            
            // Act - manual unsubscribe
            eventBus.Unsubscribe<GlobalTestEvent>(callback);
            
            // Verify unsubscription
            eventBus.Publish(new GlobalTestEvent { Message = "After unsubscribe" });
            Assert.AreEqual(1, eventCount, "Event should NOT be received after unsubscribe");
        }
        
        [Test]
        public void Unsubscribe_Manual_WrongContext_ShouldNotAffectSubscription()
        {
            // Arrange
            string correctTarget = "correct_target";
            string wrongTarget = "wrong_target";
            int eventCount = 0;
            Action<TargetedTestEvent> callback = evt => eventCount++;
            
            // Subscribe with correct context
            using (EventContext.SetContext(correctTarget))
            {
                eventBus.Subscribe<TargetedTestEvent>(callback);
            }
            
            // Verify subscription works
            eventBus.Publish(new TargetedTestEvent { TargetId = correctTarget, Message = "Test" });
            Assert.AreEqual(1, eventCount, "Event should be received");
            
            // Act - try to unsubscribe with wrong context
            using (EventContext.SetContext(wrongTarget))
            {
                eventBus.Unsubscribe<TargetedTestEvent>(callback);
            }
            
            // Verify subscription still works
            eventBus.Publish(new TargetedTestEvent { TargetId = correctTarget, Message = "Test 2" });
            Assert.AreEqual(2, eventCount, "Subscription should still work after wrong context unsubscribe");
        }
        
        [Test]
        public void Unsubscribe_Manual_WithNullCallback_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => {
                eventBus.Unsubscribe<GlobalTestEvent>(null);
            });
        }
        
        [Test]
        public void Unsubscribe_Manual_NonExistentCallback_ShouldNotThrow()
        {
            // Arrange
            Action<GlobalTestEvent> callback = evt => { };
            
            // Act & Assert - should not throw even if callback was never subscribed
            Assert.DoesNotThrow(() => {
                eventBus.Unsubscribe<GlobalTestEvent>(callback);
            });
        }
        
        // =============================================================================
        // CONTEXT MANAGEMENT TESTS
        // =============================================================================
        
        [Test]
        public void Subscribe_WithoutContext_TargetedEvent_ShouldFallbackToGlobal()
        {
            // Arrange
            int eventCount = 0;
    
            // Subscribe without context for targeted event
            eventBus.Subscribe<TargetedTestEvent>(evt => eventCount++);
    
            // Act - publish targeted event
            eventBus.Publish(new TargetedTestEvent { TargetId = "some_target", Message = "Test" });
    
            // Assert - should be treated as global and receive the event via fallback
            Assert.AreEqual(1, eventCount, "Targeted event should fallback to global subscribers when no targeted subscribers found");
        }
        
        [Test]
        public void TokenDispose_PreservesOriginalContext()
        {
            // Arrange
            string originalTarget = "original_target";
            string differentTarget = "different_target";
            int eventCount = 0;
            ISubscriptionToken token;
            
            using (EventContext.SetContext(originalTarget))
            {
                token = eventBus.Subscribe<TargetedTestEvent>(evt => eventCount++);
            }
            
            // Change context
            using (EventContext.SetContext(differentTarget))
            {
                // Verify subscription still works for original target
                eventBus.Publish(new TargetedTestEvent { TargetId = originalTarget, Message = "Test" });
                Assert.AreEqual(1, eventCount, "Should receive event for original target");
                
                // Act - dispose token while in different context
                token.Dispose();
            }
            
            // Verify unsubscription worked regardless of current context
            eventBus.Publish(new TargetedTestEvent { TargetId = originalTarget, Message = "Test 2" });
            Assert.AreEqual(1, eventCount, "Should not receive event after dispose");
        }
        
        // =============================================================================
        // MIXED TOKEN AND MANUAL UNSUBSCRIBE TESTS
        // =============================================================================
        
        [Test]
        public void TokenAndManual_SameCallback_BothShouldWork()
        {
            // Arrange
            string targetId = "mixed_test";
            int eventCount = 0;
            Action<TargetedTestEvent> callback = evt => eventCount++;
            
            ISubscriptionToken token;
            using (EventContext.SetContext(targetId))
            {
                token = eventBus.Subscribe<TargetedTestEvent>(callback);
            }
            
            // Verify subscription works
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Test 1" });
            Assert.AreEqual(1, eventCount, "Event should be received");
            
            // Act - manual unsubscribe with correct context
            using (EventContext.SetContext(targetId))
            {
                eventBus.Unsubscribe<TargetedTestEvent>(callback);
            }
            
            // Verify manual unsubscribe worked
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Test 2" });
            Assert.AreEqual(1, eventCount, "Event should NOT be received after manual unsubscribe");
            
            // Act - dispose token (should not throw even though already unsubscribed)
            Assert.DoesNotThrow(() => token.Dispose());
            Assert.IsTrue(token.IsDisposed, "Token should be marked as disposed");
        }
        
        // =============================================================================
        // STRESS TESTS
        // =============================================================================
        
        [Test]
        public void Subscribe_ManyCallbacks_ShouldAllWork()
        {
            // Arrange
            const int callbackCount = 100;
            string targetId = "stress_target";
            var eventCounts = new int[callbackCount];
            var tokens = new List<ISubscriptionToken>();
            
            using (EventContext.SetContext(targetId))
            {
                for (int i = 0; i < callbackCount; i++)
                {
                    int index = i; // Capture for closure
                    tokens.Add(eventBus.Subscribe<TargetedTestEvent>(evt => eventCounts[index]++));
                }
            }
            
            // Act
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Stress test" });
            
            // Assert
            for (int i = 0; i < callbackCount; i++)
            {
                Assert.AreEqual(1, eventCounts[i], $"Callback {i} should have been called once");
            }
            
            // Cleanup
            foreach (var token in tokens)
            {
                token.Dispose();
            }
        }
        
        [Test]
        public void Subscribe_ManyTargets_ShouldAllWorkIndependently()
        {
            // Arrange
            const int targetCount = 50;
            var eventCounts = new int[targetCount];
            var tokens = new List<ISubscriptionToken>();
            
            for (int i = 0; i < targetCount; i++)
            {
                string targetId = $"target_{i}";
                int index = i; // Capture for closure
                
                using (EventContext.SetContext(targetId))
                {
                    tokens.Add(eventBus.Subscribe<TargetedTestEvent>(evt => eventCounts[index]++));
                }
            }
            
            // Act - publish to every other target
            for (int i = 0; i < targetCount; i += 2)
            {
                eventBus.Publish(new TargetedTestEvent { TargetId = $"target_{i}", Message = $"Test {i}" });
            }
            
            // Assert
            for (int i = 0; i < targetCount; i++)
            {
                int expected = (i % 2 == 0) ? 1 : 0; // Only even targets should receive events
                Assert.AreEqual(expected, eventCounts[i], $"Target {i} should have received {expected} events");
            }
            
            // Cleanup
            foreach (var token in tokens)
            {
                token.Dispose();
            }
        }
        
        // =============================================================================
        // EDGE CASES
        // =============================================================================
        
        [Test]
        public void Subscribe_SameCallbackMultipleTimes_ShouldReceiveMultipleTimes()
        {
            // Arrange
            string targetId = "duplicate_callback";
            int eventCount = 0;
            Action<TargetedTestEvent> callback = evt => eventCount++;
            
            using (EventContext.SetContext(targetId))
            {
                eventBus.Subscribe<TargetedTestEvent>(callback);
                eventBus.Subscribe<TargetedTestEvent>(callback); // Same callback twice
                eventBus.Subscribe<TargetedTestEvent>(callback); // Same callback thrice
            }
            
            // Act
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Duplicate test" });
            
            // Assert
            Assert.AreEqual(3, eventCount, "Callback should be called 3 times (once per subscription)");
        }
        
        [Test]
        public void TokenDispose_DuringEventHandling_ShouldNotCauseIssues()
        {
            // Arrange
            string targetId = "dispose_during_handling";
            int eventCount = 0;
            ISubscriptionToken token = null;
            
            using (EventContext.SetContext(targetId))
            {
                token = eventBus.Subscribe<TargetedTestEvent>(evt => {
                    eventCount++;
                    token.Dispose(); // Dispose during event handling
                });
            }
            
            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => {
                eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "Self-dispose test" });
            });
            
            Assert.AreEqual(1, eventCount, "Event should be handled once");
            Assert.IsTrue(token.IsDisposed, "Token should be disposed");
            
            // Verify subsequent events are not received
            eventBus.Publish(new TargetedTestEvent { TargetId = targetId, Message = "After dispose" });
            Assert.AreEqual(1, eventCount, "No more events should be received");
        }
    }
}