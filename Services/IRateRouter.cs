using System.Threading.Tasks;

namespace CryptoDayTraderSuite.Services
{
    public interface IRateRouter
    {
        decimal Mid(string baseAsset, string quoteAsset);
        Task<decimal> MidAsync(string baseAsset, string quoteAsset);
        decimal Convert(string fromAsset, string toAsset, decimal amount);
        Task<decimal> ConvertAsync(string fromAsset, string toAsset, decimal amount);
    }
}