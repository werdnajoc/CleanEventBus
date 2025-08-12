using System;

namespace CleanEventBus.Interfaces
{
    public interface IApplicationEventBus
    {
        void Subscribe<T>(Action<T> callback) where T : class, IApplicationEvent;
        void Unsubscribe<T>(Action<T> callback) where T : class, IApplicationEvent;
        void Publish<T>(T @event) where T : class, IApplicationEvent;
    }
}