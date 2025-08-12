using System;
using CleanEventBus.Core;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Adapter
{
    public class AdapterEventBus: InMemoryBaseEventBus<IAdapterEvent>, IAdapterEventBus
    {
        public new void Subscribe<T>(Action<T> callback) where T : class, IAdapterEvent
        {
            base.Subscribe(callback);
        }
        
        public new void Unsubscribe<T>(Action<T> callback) where T : class, IAdapterEvent
        {
            base.Unsubscribe(callback);
        }
        
        public new void Publish<T>(T @event) where T : class, IAdapterEvent
        {
            base.Publish(@event);
        }
    }
}