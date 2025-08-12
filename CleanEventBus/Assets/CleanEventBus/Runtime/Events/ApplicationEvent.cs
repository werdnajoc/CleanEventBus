using System;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Application
{
    public abstract class ApplicationEvent : IApplicationEvent
    {
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}