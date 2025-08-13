using System;
using CleanEventBus.Core;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Domain
{
    public class DomainEventBus: InMemoryBaseEventBus<IDomainEvent>, IDomainEventBus
    {
        public new ISubscriptionToken Subscribe<T>(Action<T> callback) where T : class, IDomainEvent
        {
            return base.Subscribe(callback);
        }
        
        public new void Unsubscribe<T>(Action<T> callback) where T : class, IDomainEvent
        {
            base.Unsubscribe(callback);
        }
        
        public new void Publish<T>(T @event) where T : class, IDomainEvent
        {
            base.Publish(@event);
        }
    }
}