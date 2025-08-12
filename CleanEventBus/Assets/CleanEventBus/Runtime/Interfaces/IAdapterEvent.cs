using System;

namespace CleanEventBus.Interfaces
{
    public interface IAdapterEvent
    {
        DateTime CreatedAt { get; }
    }
}