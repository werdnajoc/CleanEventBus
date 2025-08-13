using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Core
{
    public abstract class InMemoryBaseEventBus<TEventInterface>
        where TEventInterface : class
    { 
        private static readonly ConcurrentDictionary<Type, string> TargetPropertyCache = new();
        
        private readonly Dictionary<Type, Dictionary<string, MulticastDelegate>> _targetedSubscriptions = new();
        private readonly Dictionary<Type, Delegate> _globalSubscriptions = new();
        private readonly object _lockObject = new();

        // =============================================================================
        // PUBLIC API
        // =============================================================================
        
        protected ISubscriptionToken Subscribe<T>(Action<T> callback) where T : class, TEventInterface
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            
            var eventType = typeof(T);
            var targetProperty = GetTargetProperty(eventType);
            var currentContext = EventContext.CurrentStoreId; // Capture current context

            lock (_lockObject)
            {
                if (targetProperty != null && !string.IsNullOrEmpty(currentContext))
                {
                    SubscribeToTargetedEventInternal(callback, currentContext);
                }
                else if (targetProperty != null && string.IsNullOrEmpty(currentContext))
                {
                    // ✅ ESTO ES LO NUEVO: Targeted event WITHOUT context - treat as global
                    // but won't receive targeted events (as expected by test)
                    SubscribeToGlobalEventInternal(callback);
                }
                else
                {
                    SubscribeToGlobalEventInternal(callback);
                }
            }
            
            // Return token that remembers the context and callback
            return new SubscriptionToken(() => {
                var oldContext = EventContext.CurrentStoreId;
                try 
                {
                    EventContext.SetContext(currentContext);
                    UnsubscribeInternal(callback);
                }
                finally 
                {
                    EventContext.SetContext(oldContext);
                }
            });
        }
        
        protected void Unsubscribe<T>(Action<T> callback) where T : class, TEventInterface
        {
            UnsubscribeInternal(callback);
        }
        
        private void UnsubscribeInternal<T>(Action<T> callback) where T : class, TEventInterface
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            
            var eventType = typeof(T);
            var targetProperty = GetTargetProperty(eventType);

            lock (_lockObject)
            {
                if (targetProperty != null && !string.IsNullOrEmpty(EventContext.CurrentStoreId))
                {
                    UnsubscribeFromTargetedEventInternal(callback, EventContext.CurrentStoreId);
                }
                else
                {
                    UnsubscribeFromGlobalEventInternal(callback);
                }
            }
        }

        protected void Publish<T>(T @event) where T : class, TEventInterface
        {
            if (@event == null) return;

            var eventType = typeof(T);
            var targetProperty = GetTargetProperty(eventType);

            // Get snapshot of delegates WITHOUT holding lock during invocation
            MulticastDelegate targetedDelegate = null;
            Delegate globalDelegate = null;
            string targetId = null;

            if (targetProperty != null)
            {
                targetId = GetTargetIdFromEvent(@event, targetProperty);
                if (!string.IsNullOrEmpty(targetId))
                {
                    lock (_lockObject)
                    {
                        if (_targetedSubscriptions.ContainsKey(eventType) &&
                            _targetedSubscriptions[eventType].ContainsKey(targetId))
                        {
                            targetedDelegate = _targetedSubscriptions[eventType][targetId];
                        }
                    }
                }
            }

            // If no targeted delegate found, get global delegate
            if (targetedDelegate == null)
            {
                lock (_lockObject)
                {
                    if (_globalSubscriptions.ContainsKey(eventType))
                    {
                        globalDelegate = _globalSubscriptions[eventType];
                    }
                }
            }

            // Invoke delegates OUTSIDE of lock to prevent deadlocks
            if (targetedDelegate != null && targetedDelegate is Action<T> targetedAction)
            {
                try
                {
                    targetedAction.Invoke(@event);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error in targeted event handler: {ex.Message}");
                }
            }
            else if (globalDelegate != null && globalDelegate is Action<T> globalAction)
            {
                try
                {
                    globalAction.Invoke(@event);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error in global event handler: {ex.Message}");
                }
            }
        }

        // =============================================================================
        // INTERNAL METHODS (all called within locks)
        // =============================================================================
        
        private void SubscribeToTargetedEventInternal<T>(Action<T> callback, string targetId) where T : class, TEventInterface
        {
            var eventType = typeof(T);

            if (!_targetedSubscriptions.ContainsKey(eventType))
                _targetedSubscriptions[eventType] = new Dictionary<string, MulticastDelegate>();

            if (!_targetedSubscriptions[eventType].ContainsKey(targetId))
                _targetedSubscriptions[eventType][targetId] = null;

            _targetedSubscriptions[eventType][targetId] = 
                (MulticastDelegate)Delegate.Combine(_targetedSubscriptions[eventType][targetId], callback);
        }

        private void UnsubscribeFromTargetedEventInternal<T>(Action<T> callback, string targetId)
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

        private void SubscribeToGlobalEventInternal<T>(Action<T> callback) where T : class, TEventInterface
        {
            var type = typeof(T);
            _globalSubscriptions.TryAdd(type, null);
                
            _globalSubscriptions[type] = Delegate.Combine(_globalSubscriptions[type], callback);
        }

        private void UnsubscribeFromGlobalEventInternal<T>(Action<T> callback) where T : class, TEventInterface
        {
            var type = typeof(T);
            if (_globalSubscriptions.ContainsKey(type))
            {
                _globalSubscriptions[type] = Delegate.Remove(_globalSubscriptions[type], callback);
            }
        }

        // =============================================================================
        // HELPER METHODS (lock-free)
        // =============================================================================
        
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
            try
            {
                var property = typeof(T).GetProperty(targetPropertyName);
                return property?.GetValue(@event)?.ToString();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error getting target ID from event: {ex.Message}");
                return null;
            }
        }
    }
}