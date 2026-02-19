using System.Threading.Tasks;

namespace CryptoDayTraderSuite.Services
{
    public interface IRateRouter
    {
        Task<decimal> MidAsync(string baseAsset, string quoteAsset);
        Task<decimal> ConvertAsync(string fromAsset, string toAsset, decimal amount);
    }
}