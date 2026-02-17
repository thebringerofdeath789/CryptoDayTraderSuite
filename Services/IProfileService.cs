namespace CryptoDayTraderSuite.Services
{
    public interface IProfileService
    {
        void Export(string path, string passphrase);
        void Import(string path, string passphrase);
    }
}