using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CryptoDayTraderSuite.Services.Messaging
{
    public interface IEventBus
    {
        void Publish<T>(T eventMessage);
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
    }

    public class EventBus : IEventBus
    {
        /* Simple singleton for static access if needed, but prefer DI */
        [Obsolete("Use Dependency Injection instead of Static Instance.")]
        private static readonly Lazy<EventBus> _instance = new Lazy<EventBus>(() => new EventBus());
        [Obsolete("Use Dependency Injection instead of Static Instance.")]
        public static EventBus Instance => _instance.Value;

        private readonly ConcurrentDictionary<Type, List<object>> _handlers = new ConcurrentDictionary<Type, List<object>>();

        public EventBus() { }

        public void Publish<T>(T eventMessage)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var handlers))
            {
                object[] snapshot;
                lock (handlers)
                {
                    snapshot = handlers.ToArray();
                }

                /* iterate a copy to be safe against modification during execution */
                foreach (var handler in snapshot)
                {
                    try
                    {
                        ((Action<T>)handler)(eventMessage);
                    }
                    catch (Exception ex)
                    {
                        /* Do not crash the bus on handler error, but maybe log it? */
                        /* Avoid infinite loop if logging fails though */
                        System.Diagnostics.Debug.WriteLine($"Error handling event {type.Name}: {ex.Message}");
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            _handlers.AddOrUpdate(type,
                t => new List<object> { handler },
                (t, list) =>
                {
                    lock (list) 
                    {
                        if (!list.Contains(handler)) list.Add(handler);
                    }
                    return list;
                });
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                lock (list)
                {
                    list.Remove(handler);
                }
            }
        }
    }
}
