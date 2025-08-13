using System;

namespace CleanEventBus.Interfaces
{
    public interface IApplicationEvent
    {
        DateTime CreatedAt { get; }
    }
}