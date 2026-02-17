using System;

namespace CryptoDayTraderSuite.Strategy
{
	public class RollingStats
	{
		private readonly double[] _buf;

		private int _idx = 0;

		private int _count = 0;

		private double _sum = 0.0;

		public RollingStats(int window)
		{
			_buf = new double[Math.Max(1, window)];
		}

		public void Add(double x)
		{
			if (_count < _buf.Length)
			{
				_buf[_idx] = x;
				_sum += x;
				_idx++;
				_count++;
			}
			else
			{
				_idx %= _buf.Length;
				_sum -= _buf[_idx];
				_buf[_idx] = x;
				_sum += x;
				_idx++;
			}
		}

		public double Mean()
		{
			if (_count == 0)
			{
				return 0.0;
			}
			int n = Math.Min(_count, _buf.Length);
			return _sum / (double)n;
		}
	}
}
