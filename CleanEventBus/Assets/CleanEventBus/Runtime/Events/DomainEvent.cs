using System;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Domain
{
    public abstract class DomainEvent : IDomainEvent
    {
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}