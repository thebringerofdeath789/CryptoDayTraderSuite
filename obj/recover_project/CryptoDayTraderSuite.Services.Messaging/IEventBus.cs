using System;

namespace CryptoDayTraderSuite.Services.Messaging
{
	public interface IEventBus
	{
		void Publish<T>(T eventMessage);

		void Subscribe<T>(Action<T> handler);

		void Unsubscribe<T>(Action<T> handler);
	}
}
