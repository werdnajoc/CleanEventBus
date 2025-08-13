using System;

namespace CleanEventBus.Interfaces
{
    public interface IDomainEventBus
    {
        ISubscriptionToken Subscribe<T>(Action<T> callback) where T : class, IDomainEvent;
        void Unsubscribe<T>(Action<T> callback) where T : class, IDomainEvent;
        void Publish<T>(T @event) where T : class, IDomainEvent;
    }
}