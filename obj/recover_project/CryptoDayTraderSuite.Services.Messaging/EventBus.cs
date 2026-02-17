#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace CryptoDayTraderSuite.Services.Messaging
{
	public class EventBus : IEventBus
	{
		[Obsolete("Use Dependency Injection instead of Static Instance.")]
		private static readonly Lazy<EventBus> _instance = new Lazy<EventBus>(() => new EventBus());

		private readonly ConcurrentDictionary<Type, List<object>> _handlers = new ConcurrentDictionary<Type, List<object>>();

		[Obsolete("Use Dependency Injection instead of Static Instance.")]
		public static EventBus Instance => _instance.Value;

		public void Publish<T>(T eventMessage)
		{
			Type type = typeof(T);
			if (!_handlers.TryGetValue(type, out var handlers))
			{
				return;
			}
			object[] snapshot;
			lock (handlers)
			{
				snapshot = handlers.ToArray();
			}
			object[] array = snapshot;
			foreach (object handler in array)
			{
				try
				{
					((Action<T>)handler)(eventMessage);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Error handling event " + type.Name + ": " + ex.Message);
				}
			}
		}

		public void Subscribe<T>(Action<T> handler)
		{
			Type type = typeof(T);
			_handlers.AddOrUpdate(type, (Type t) => new List<object> { handler }, delegate(Type t, List<object> list)
			{
				lock (list)
				{
					if (!list.Contains(handler))
					{
						list.Add(handler);
					}
				}
				return list;
			});
		}

		public void Unsubscribe<T>(Action<T> handler)
		{
			Type type = typeof(T);
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
