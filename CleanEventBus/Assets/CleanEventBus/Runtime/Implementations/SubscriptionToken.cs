using System;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Core
{
    internal class SubscriptionToken : ISubscriptionToken
    {
        private readonly Action _unsubscribeAction;
        private bool _disposed;
        
        public bool IsDisposed => _disposed;
        
        public SubscriptionToken(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _unsubscribeAction.Invoke();
                }
                catch (Exception ex)
                {
                    // Log error but don't throw from Dispose
                    UnityEngine.Debug.LogError($"Error during unsubscribe: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}