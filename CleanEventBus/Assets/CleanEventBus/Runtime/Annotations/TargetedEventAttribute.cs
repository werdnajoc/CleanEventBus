using System;

namespace CleanEventBus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TargetedEventAttribute : Attribute
    {
        public string TargetProperty { get; }
        
        public TargetedEventAttribute(string targetProperty)
        {
            TargetProperty = targetProperty;
        }
    }
}