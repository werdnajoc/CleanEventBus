// =============================================================================
// Unit Tests for Targeted Events with Different Properties
// =============================================================================
using System.Collections.Generic;
using CleanEventBus.Application;
using CleanEventBus.Core;
using CleanEventBus.Interfaces;
using NUnit.Framework;

namespace CleanEventBus.Tests.Runtime
{
    public class TargetedEventDifferentPropertiesTests
    {
        private ApplicationEventBus _eventBus;

        // =============================================================================
        // TEST EVENTS WITH DIFFERENT TARGET PROPERTIES
        // =============================================================================

        // Standard StoreId targeting
        [TargetedEvent("StoreId")]
        private class StoreEvent : ApplicationEvent
        {
            public string StoreId { get; set; }
        }

        // UserId targeting
        [TargetedEvent("UserId")]
        private class UserEvent : ApplicationEvent
        {
            public string UserId { get; set; }
        }

        // PlayerId targeting
        [TargetedEvent("PlayerId")]
        private class PlayerEvent : ApplicationEvent
        {
            public string PlayerId { get; set; }
            public int Score { get; set; }
            public string Achievement { get; set; }
        }

        // BuildingId targeting
        [TargetedEvent("BuildingId")]
        private class BuildingEvent : ApplicationEvent
        {
            public string BuildingId { get; set; }
            public int Level { get; set; }
            public decimal Revenue { get; set; }
        }

        // SessionId targeting
        [TargetedEvent("SessionId")]
        private class SessionEvent : ApplicationEvent
        {
            public string Action { get; set; }
        }

        // Custom property name
        [TargetedEvent("CustomTargetProperty")]
        private class CustomTargetEvent : ApplicationEvent
        {
            public string CustomTargetProperty { get; set; }
            public string Data { get; set; }
        }

        // Numeric targeting (should work as string)
        [TargetedEvent("EntityId")]
        private class NumericTargetEvent : ApplicationEvent
        {
            public int EntityId { get; set; }
            public string Description { get; set; }
        }

        // Global event (no targeting)
        private class GlobalEvent : ApplicationEvent
        {
            public string GlobalMessage { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            _eventBus = new ApplicationEventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _eventBus = null;
        }

        // =============================================================================
        // BASIC TARGETING TESTS
        // =============================================================================

        [Test]
        public void Should_Target_Events_By_UserId()
        {
            // Arrange
            string targetUserId = "user_123";
            string otherUserId = "user_456";
            bool targetUserReceived = false;
            bool otherUserReceived = false;

            // Subscribe target user
            using (EventContext.SetContext(targetUserId))
            {
                _eventBus.Subscribe<UserEvent>(evt =>
                {
                    targetUserReceived = true;
                    Assert.AreEqual(targetUserId, evt.UserId);
                });
            }

            // Subscribe other user
            using (EventContext.SetContext(otherUserId))
            {
                _eventBus.Subscribe<UserEvent>(_ => { otherUserReceived = true; });
            }

            // Act - publish event for target user
            _eventBus.Publish(new UserEvent
            {
                UserId = targetUserId
            });

            // Assert
            Assert.IsTrue(targetUserReceived, "Target user should receive the event");
            Assert.IsFalse(otherUserReceived, "Other user should NOT receive the event");
        }

        [Test]
        public void Should_Target_Events_By_PlayerId()
        {
            // Arrange
            string player1 = "player_abc";
            string player2 = "player_xyz";
            PlayerEvent receivedEvent = null;
            int eventsReceived = 0;

            // Subscribe player 1
            using (EventContext.SetContext(player1))
            {
                _eventBus.Subscribe<PlayerEvent>(evt =>
                {
                    receivedEvent = evt;
                    eventsReceived++;
                });
            }

            // Subscribe player 2
            using (EventContext.SetContext(player2))
            {
                _eventBus.Subscribe<PlayerEvent>(_ => { eventsReceived++; });
            }

            // Act - publish event for player 1
            _eventBus.Publish(new PlayerEvent
            {
                PlayerId = player1,
                Score = 1000,
                Achievement = "First Kill"
            });

            // Assert
            Assert.AreEqual(1, eventsReceived, "Only one player should receive the event");
            Assert.IsNotNull(receivedEvent, "Event should be received");
            Assert.AreEqual(player1, receivedEvent.PlayerId);
            Assert.AreEqual(1000, receivedEvent.Score);
            Assert.AreEqual("First Kill", receivedEvent.Achievement);
        }

        [Test]
        public void Should_Target_Events_By_BuildingId()
        {
            // Arrange
            string building1 = "building_tower_01";
            string building2 = "building_factory_02";
            string building3 = "building_shop_03";

            var receivedEvents = new List<BuildingEvent>();

            // Subscribe multiple buildings
            using (EventContext.SetContext(building1))
            {
                _eventBus.Subscribe<BuildingEvent>(evt => receivedEvents.Add(evt));
            }

            using (EventContext.SetContext(building2))
            {
                _eventBus.Subscribe<BuildingEvent>(evt => receivedEvents.Add(evt));
            }

            using (EventContext.SetContext(building3))
            {
                _eventBus.Subscribe<BuildingEvent>(evt => receivedEvents.Add(evt));
            }

            // Act - publish events for different buildings
            _eventBus.Publish(new BuildingEvent { BuildingId = building2, Level = 3, Revenue = 500.50m });
            _eventBus.Publish(new BuildingEvent { BuildingId = building1, Level = 1, Revenue = 100.25m });

            // Assert
            Assert.AreEqual(2, receivedEvents.Count, "Should receive exactly 2 events");

            // Verify first event (building2)
            var event1 = receivedEvents.Find(e => e.BuildingId == building2);
            Assert.IsNotNull(event1, "Building2 event should be received");
            Assert.AreEqual(3, event1.Level);
            Assert.AreEqual(500.50m, event1.Revenue);

            // Verify second event (building1)
            var event2 = receivedEvents.Find(e => e.BuildingId == building1);
            Assert.IsNotNull(event2, "Building1 event should be received");
            Assert.AreEqual(1, event2.Level);
            Assert.AreEqual(100.25m, event2.Revenue);

            // Verify building3 didn't receive anything
            var event3 = receivedEvents.Find(e => e.BuildingId == building3);
            Assert.IsNull(event3, "Building3 should not receive any events");
        }

        // =============================================================================
        // CUSTOM PROPERTY TESTS
        // =============================================================================

        [Test]
        public void Should_Target_Events_By_CustomProperty()
        {
            // Arrange
            string customTarget = "custom_target_abc123";
            bool eventReceived = false;
            CustomTargetEvent receivedEvent = null;

            // Subscribe with custom target
            using (EventContext.SetContext(customTarget))
            {
                _eventBus.Subscribe<CustomTargetEvent>(evt =>
                {
                    eventReceived = true;
                    receivedEvent = evt;
                });
            }

            // Act
            _eventBus.Publish(new CustomTargetEvent
            {
                CustomTargetProperty = customTarget,
                Data = "Custom targeting works!"
            });

            // Assert
            Assert.IsTrue(eventReceived, "Event should be received");
            Assert.IsNotNull(receivedEvent, "Received event should not be null");
            Assert.AreEqual(customTarget, receivedEvent.CustomTargetProperty);
            Assert.AreEqual("Custom targeting works!", receivedEvent.Data);
        }

        [Test]
        public void Should_Target_Events_By_SessionId()
        {
            // Arrange
            string session1 = "session_morning_001";
            string session2 = "session_evening_002";

            var session1Events = new List<SessionEvent>();
            var session2Events = new List<SessionEvent>();

            // Subscribe sessions
            using (EventContext.SetContext(session1))
            {
                _eventBus.Subscribe<SessionEvent>(evt => session1Events.Add(evt));
            }

            using (EventContext.SetContext(session2))
            {
                _eventBus.Subscribe<SessionEvent>(evt => session2Events.Add(evt));
            }

            // Act - multiple events for different sessions
            _eventBus.Publish(new SessionEvent { Action = "LOGIN" });
            _eventBus.Publish(new SessionEvent
                {
                    Action = "PURCHASE"
                });
            _eventBus.Publish(new SessionEvent
                {
                    Action = "LOGIN"
                });

            // Assert
            Assert.AreEqual(2, session1Events.Count, "Session 1 should receive 2 events");
            Assert.AreEqual(1, session2Events.Count, "Session 2 should receive 1 event");

            Assert.AreEqual("LOGIN", session1Events[0].Action);
            Assert.AreEqual("PURCHASE", session1Events[1].Action);
            Assert.AreEqual("LOGIN", session2Events[0].Action);
        }

        // =============================================================================
        // NUMERIC TARGET TESTS
        // =============================================================================

        [Test]
        public void Should_Target_Events_By_NumericId()
        {
            // Arrange
            string entityId1 = "123"; // numeric as string in context
            string entityId2 = "456";

            NumericTargetEvent receivedEvent = null;
            int eventsReceived = 0;

            // Subscribe to entity 123
            using (EventContext.SetContext(entityId1))
            {
                _eventBus.Subscribe<NumericTargetEvent>(evt =>
                {
                    receivedEvent = evt;
                    eventsReceived++;
                });
            }

            // Subscribe to entity 456
            using (EventContext.SetContext(entityId2))
            {
                _eventBus.Subscribe<NumericTargetEvent>(_ => { eventsReceived++; });
            }

            // Act - publish for entity 123
            _eventBus.Publish(new NumericTargetEvent
            {
                EntityId = 123, // numeric property
                Description = "Numeric targeting test"
            });

            // Assert
            Assert.AreEqual(1, eventsReceived, "Only one subscriber should receive the event");
            Assert.IsNotNull(receivedEvent, "Event should be received");
            Assert.AreEqual(123, receivedEvent.EntityId);
            Assert.AreEqual("Numeric targeting test", receivedEvent.Description);
        }

        // =============================================================================
        // MIXED TARGETING TESTS
        // =============================================================================

        [Test]
        public void Should_Handle_Multiple_Different_Target_Types_Simultaneously()
        {
            // Arrange
            var receivedEvents = new List<string>();

            // Subscribe to different target types
            using (EventContext.SetContext("user_001"))
            {
                _eventBus.Subscribe<UserEvent>(evt => receivedEvents.Add($"User-{evt.UserId}"));
            }

            using (EventContext.SetContext("player_001"))
            {
                _eventBus.Subscribe<PlayerEvent>(evt => receivedEvents.Add($"Player-{evt.PlayerId}"));
            }

            using (EventContext.SetContext("building_001"))
            {
                _eventBus.Subscribe<BuildingEvent>(evt => receivedEvents.Add($"Building-{evt.BuildingId}"));
            }

            // Act - publish different event types
            _eventBus.Publish(new UserEvent { UserId = "user_001" });
            _eventBus.Publish(new PlayerEvent { PlayerId = "player_001", Score = 500 });
            _eventBus.Publish(new BuildingEvent { BuildingId = "building_001", Level = 2 });

            // Wrong targets - should not be received
            _eventBus.Publish(new UserEvent { UserId = "user_002" });
            _eventBus.Publish(new PlayerEvent { PlayerId = "player_002", Score = 300 });

            // Assert
            Assert.AreEqual(3, receivedEvents.Count, "Should receive exactly 3 events");
            Assert.Contains("User-user_001", receivedEvents);
            Assert.Contains("Player-player_001", receivedEvents);
            Assert.Contains("Building-building_001", receivedEvents);
        }

        // =============================================================================
        // GLOBAL vs TARGETED MIXED TESTS
        // =============================================================================

        [Test]
        public void Should_Handle_Global_And_Targeted_Events_Together()
        {
            // Arrange
            var allEvents = new List<string>();

            // Subscribe to targeted events
            using (EventContext.SetContext("store_001"))
            {
                _eventBus.Subscribe<StoreEvent>(evt => allEvents.Add($"Targeted-{evt.StoreId}"));
            }

            // Subscribe to global events (no context)
            _eventBus.Subscribe<GlobalEvent>(evt => allEvents.Add($"Global-{evt.GlobalMessage}"));

            // Act
            _eventBus.Publish(new StoreEvent { StoreId = "store_001" });
            _eventBus.Publish(new GlobalEvent { GlobalMessage = "Global event" });
            _eventBus.Publish(new StoreEvent
                { StoreId = "store_002"
                }); // Should not be received

            // Assert
            Assert.AreEqual(2, allEvents.Count, "Should receive targeted + global events");
            Assert.Contains("Targeted-store_001", allEvents);
            Assert.Contains("Global-Global event", allEvents);
        }

        // =============================================================================
        // TOKEN DISPOSAL TESTS
        // =============================================================================

        [Test]
        public void Should_Properly_Dispose_Tokens_For_Different_Target_Properties()
        {
            // Arrange
            var tokens = new List<ISubscriptionToken>();
            var eventsReceived = 0;

            // Subscribe to different target types and collect tokens
            using (EventContext.SetContext("user_123"))
            {
                tokens.Add(_eventBus.Subscribe<UserEvent>(_ => eventsReceived++));
            }

            using (EventContext.SetContext("player_456"))
            {
                tokens.Add(_eventBus.Subscribe<PlayerEvent>(_ => eventsReceived++));
            }

            using (EventContext.SetContext("building_789"))
            {
                tokens.Add(_eventBus.Subscribe<BuildingEvent>(_ => eventsReceived++));
            }

            // Verify subscriptions work
            _eventBus.Publish(new UserEvent { UserId = "user_123" });
            _eventBus.Publish(new PlayerEvent { PlayerId = "player_456" });
            _eventBus.Publish(new BuildingEvent { BuildingId = "building_789" });

            Assert.AreEqual(3, eventsReceived, "All subscriptions should work before disposal");

            // Act - dispose tokens
            foreach (var token in tokens)
            {
                token.Dispose();
            }

            // Reset counter
            eventsReceived = 0;

            // Publish same events again
            _eventBus.Publish(new UserEvent { UserId = "user_123" });
            _eventBus.Publish(new PlayerEvent { PlayerId = "player_456" });
            _eventBus.Publish(new BuildingEvent { BuildingId = "building_789" });

            // Assert
            Assert.AreEqual(0, eventsReceived, "No events should be received after token disposal");
        }

        // =============================================================================
        // ERROR CASES TESTS
        // =============================================================================

        [Test]
        public void Should_Handle_Event_With_Null_Target_Property()
        {
            // Arrange
            bool eventReceived = false;

            using (EventContext.SetContext("user_123"))
            {
                _eventBus.Subscribe<UserEvent>(_ => eventReceived = true);
            }

            // Act - publish event with null target property
            _eventBus.Publish(new UserEvent { UserId = null });

            // Assert - should fall back to global behavior
            Assert.IsFalse(eventReceived, "Targeted subscriber should not receive event with null target");
        }

        [Test]
        public void Should_Handle_Event_With_Empty_Target_Property()
        {
            // Arrange
            bool eventReceived = false;

            using (EventContext.SetContext("user_123"))
            {
                _eventBus.Subscribe<UserEvent>(_ => eventReceived = true);
            }

            // Act - publish event with empty target property
            _eventBus.Publish(new UserEvent { UserId = "" });

            // Assert - should fall back to global behavior
            Assert.IsFalse(eventReceived, "Targeted subscriber should not receive event with empty target");
        }

        [Test]
        public void Should_Handle_Multiple_Subscribers_Same_Target_Different_Properties()
        {
            // Arrange
            string targetId = "target_123";
            var receivedEvents = new List<string>();

            // Subscribe same target to different event types
            using (EventContext.SetContext(targetId))
            {
                _eventBus.Subscribe<UserEvent>(evt => receivedEvents.Add($"User-{evt.UserId}"));
                _eventBus.Subscribe<PlayerEvent>(evt => receivedEvents.Add($"Player-{evt.PlayerId}"));
            }

            // Act
            _eventBus.Publish(new UserEvent { UserId = targetId });
            _eventBus.Publish(new PlayerEvent { PlayerId = targetId, Score = 100 });
            _eventBus.Publish(new BuildingEvent { BuildingId = targetId, Level = 1 }); // No subscriber

            // Assert
            Assert.AreEqual(2, receivedEvents.Count, "Should receive events for subscribed types only");
            Assert.Contains($"User-{targetId}", receivedEvents);
            Assert.Contains($"Player-{targetId}", receivedEvents);
        }
    }
}