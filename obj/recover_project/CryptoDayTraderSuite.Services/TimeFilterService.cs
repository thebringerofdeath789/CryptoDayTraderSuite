using System.Collections.Generic;

namespace CryptoDayTraderSuite.Services
{
	public class TimeFilterService : ITimeFilterService
	{
		private readonly HashSet<int> _blockedHours = new HashSet<int>();

		public void BlockHour(int hour)
		{
			if (!_blockedHours.Contains(hour))
			{
				_blockedHours.Add(hour);
			}
		}

		public bool IsTradableHour(int hour)
		{
			return !_blockedHours.Contains(hour);
		}

		public void Clear()
		{
			_blockedHours.Clear();
		}
	}
}
