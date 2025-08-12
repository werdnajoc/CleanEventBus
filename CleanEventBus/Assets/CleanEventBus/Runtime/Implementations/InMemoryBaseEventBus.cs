
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace CleanEventBus.Core
{
    public abstract class InMemoryBaseEventBus<TEventInterface>
        where TEventInterface : class
    { 
        private static readonly ConcurrentDictionary<Type, string> TargetPropertyCache = new();
        
        private readonly Dictionary<Type, Dictionary<string, MulticastDelegate>> _targetedSubscriptions = new();
        private readonly Dictionary<Type, Delegate> _globalSubscriptions = new();
        private readonly object _lockObject = new();

        protected void Subscribe<T>(Action<T> callback) where T : class, TEventInterface
        {
            var eventType = typeof(T);
            var targetProperty = GetTargetProperty(eventType);

            lock (_lockObject)
            {
                if (targetProperty != null && !string.IsNullOrEmpty(EventContext.CurrentStoreId))
                {
                    SubscribeToTargetedEvent(callback, EventContext.CurrentStoreId);
                }
                else
                {
                    SubscribeToGlobalEvent(callback);
                }
            }
        }

        protected void Unsubscribe<T>(Action<T> callback) where T : class, TEventInterface
        {
            var eventType = typeof(T);
            var targetProperty = GetTargetProperty(eventType);

            lock (_lockObject)
            {
                if (targetProperty != null && !string.IsNullOrEmpty(EventContext.CurrentStoreId))
                {
                    UnsubscribeFromTargetedEvent(callback, EventContext.CurrentStoreId);
                }
                else
                {
                    UnsubscribeFromGlobalEvent(callback);
                }
            }
        }

        protected void Publish<T>(T @event) where T : class, TEventInterface
        {
            if (@event == null) return;

            var eventType = typeof(T);
            var targetProperty = GetTargetProperty(eventType);

            if (targetProperty != null)
            {
                var targetId = GetTargetIdFromEvent(@event, targetProperty);
                if (!string.IsNullOrEmpty(targetId))
                {
                    PublishTargetedEvent(@event, targetId);
                    return;
                }
            }

            PublishGlobalEvent(@event);
        }

        // MÉTODOS PRIVADOS
        private void SubscribeToTargetedEvent<T>(Action<T> callback, string targetId) where T : class, TEventInterface
        {
            var eventType = typeof(T);

            if (!_targetedSubscriptions.ContainsKey(eventType))
                _targetedSubscriptions[eventType] = new Dictionary<string, MulticastDelegate>();

            if (!_targetedSubscriptions[eventType].ContainsKey(targetId))
                _targetedSubscriptions[eventType][targetId] = null;

            _targetedSubscriptions[eventType][targetId] = 
                (MulticastDelegate)Delegate.Combine(_targetedSubscriptions[eventType][targetId], callback);
        }

        private void UnsubscribeFromTargetedEvent<T>(Action<T> callback, string targetId)
            where T : class, TEventInterface
        {
            var eventType = typeof(T);

            if (_targetedSubscriptions.ContainsKey(eventType) &&
                _targetedSubscriptions[eventType].ContainsKey(targetId))
            {
                _targetedSubscriptions[eventType][targetId] = 
                    (MulticastDelegate)Delegate.Remove(_targetedSubscriptions[eventType][targetId], callback);

                // Limpiar si no quedan callbacks
                if (_targetedSubscriptions[eventType][targetId] == null)
                {
                    _targetedSubscriptions[eventType].Remove(targetId);
                    if (_targetedSubscriptions[eventType].Count == 0)
                    {
                        _targetedSubscriptions.Remove(eventType);
                    }
                }
            }
        }

        private void PublishTargetedEvent<T>(T @event, string targetId) where T : class, TEventInterface
        {
            var eventType = typeof(T);

            lock (_lockObject)
            {
                if (_targetedSubscriptions.ContainsKey(eventType) &&
                    _targetedSubscriptions[eventType].ContainsKey(targetId) &&
                    _targetedSubscriptions[eventType][targetId] is Action<T> action)
                {
                    action.Invoke(@event);
                }
            }
        }

        private void PublishGlobalEvent<T>(T @event) where T : class, TEventInterface
        {
            var type = typeof(T);
            if (_globalSubscriptions.ContainsKey(type) && 
                _globalSubscriptions[type] is Action<T> action)
            {
                action.Invoke(@event);
            }
        }

        private void SubscribeToGlobalEvent<T>(Action<T> callback) where T : class, TEventInterface
        {
            var type = typeof(T);
            _globalSubscriptions.TryAdd(type, null);
                
            _globalSubscriptions[type] = Delegate.Combine(_globalSubscriptions[type], callback);
        }

        private void UnsubscribeFromGlobalEvent<T>(Action<T> callback) where T : class, TEventInterface
        {
            var type = typeof(T);
            if (_globalSubscriptions.ContainsKey(type))
            {
                _globalSubscriptions[type] = Delegate.Remove(_globalSubscriptions[type], callback);
            }
        }

        private static string GetTargetProperty(Type eventType)
        {
            return TargetPropertyCache.GetOrAdd(eventType, type =>
            {
                var attr = type.GetCustomAttribute<TargetedEventAttribute>();
                return attr?.TargetProperty;
            });
        }

        private static string GetTargetIdFromEvent<T>(T @event, string targetPropertyName)
            where T : class, TEventInterface
        {
            var property = typeof(T).GetProperty(targetPropertyName);
            return property?.GetValue(@event)?.ToString();
        }
    }
}