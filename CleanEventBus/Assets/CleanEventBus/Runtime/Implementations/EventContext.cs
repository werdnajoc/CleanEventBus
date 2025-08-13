using System;
using System.Threading;

namespace CleanEventBus.Core
{
    public class EventContext
    {
        private static readonly ThreadLocal<string> currentStoreId = new ThreadLocal<string>();
        
        public static string CurrentStoreId
        {
            get => currentStoreId.Value;
            private set => currentStoreId.Value = value;
        }
        
        public static IDisposable SetContext(string storeId)
        {
            var previousContext = CurrentStoreId;
            CurrentStoreId = storeId;
            return new ContextScope(previousContext);
        }
        
        private class ContextScope : IDisposable
        {
            private readonly string _previousContext;
            
            public ContextScope(string previousContext)
            {
                this._previousContext = previousContext;
            }
            
            public void Dispose()
            {
                CurrentStoreId = _previousContext;
            }
        }
    }
}