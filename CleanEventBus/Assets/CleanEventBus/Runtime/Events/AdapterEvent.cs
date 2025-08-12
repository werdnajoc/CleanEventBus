using System;
using CleanEventBus.Interfaces;

namespace CleanEventBus.Adapter
{
    public abstract class AdapterEvent : IAdapterEvent
    {
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}