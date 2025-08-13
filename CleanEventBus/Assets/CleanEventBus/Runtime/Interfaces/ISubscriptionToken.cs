using System;

namespace CleanEventBus.Interfaces
{
    /// <summary>
    /// Represents a subscription that can be disposed to automatically unsubscribe
    /// </summary>
    public interface ISubscriptionToken : IDisposable
    {
        /// <summary>
        /// Whether this subscription has been disposed
        /// </summary>
        bool IsDisposed { get; }
    }
}