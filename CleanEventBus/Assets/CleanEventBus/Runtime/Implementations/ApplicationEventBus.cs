using System;
using CleanEventBus.Core;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Application
{
    public class ApplicationEventBus: InMemoryBaseEventBus<IApplicationEvent>, IApplicationEventBus
    {
        public new void Subscribe<T>(Action<T> callback) where T : class, IApplicationEvent
        {
            base.Subscribe(callback);
        }
        
        public new void Unsubscribe<T>(Action<T> callback) where T : class, IApplicationEvent
        {
            base.Unsubscribe(callback);
        }
        
        public new void Publish<T>(T @event) where T : class, IApplicationEvent
        {
            base.Publish(@event);
        }
    }
}