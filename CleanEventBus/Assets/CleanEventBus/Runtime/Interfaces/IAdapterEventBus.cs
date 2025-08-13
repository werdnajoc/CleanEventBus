using System;

namespace CleanEventBus.Interfaces
{
    public interface IAdapterEventBus
    {
        ISubscriptionToken Subscribe<T>(Action<T> callback) where T : class, IAdapterEvent;
        void Unsubscribe<T>(Action<T> callback) where T : class, IAdapterEvent;
        void Publish<T>(T @event) where T : class, IAdapterEvent;
    }
}