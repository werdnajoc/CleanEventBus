using System;

namespace CleanEventBus.Interfaces
{
    public interface IDomainEvent
    {
        DateTime CreatedAt { get; }
    }
}