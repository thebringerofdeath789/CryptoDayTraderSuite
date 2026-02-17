using System.Collections.Generic;

namespace CryptoDayTraderSuite.Services
{
    public interface ITimeFilterService
    {
        void BlockHour(int hour);
        bool IsTradableHour(int hour);
        void Clear();
    }
}