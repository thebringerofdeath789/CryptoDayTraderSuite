using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public class ProfileService : IProfileService
    {
        private const string Magic = "CDTPv1\0";

        private readonly IAccountService _accountService;
        private readonly IKeyService _keyService;
        private readonly ITimeFilterService _timeFilterService;

        public ProfileService(IAccountService accountService, IKeyService keyService, ITimeFilterService timeFilterService)
        {
            _accountService = accountService;
            _keyService = keyService;
            _timeFilterService = timeFilterService;
        }

        public void Export(string path, string passphrase)
        {
            Log.Info($"Exporting profile to {path}");
            var pd = new ProfileData();
            pd.Accounts = _accountService.GetAll();

            var keys = _keyService.GetAll();
            pd.Keys = new List<KeyInfo>();
            foreach (var k in keys)
            {
                var copy = new KeyInfo 
                {
                    Broker = k.Broker,
                    Label = k.Label,
                    CreatedUtc = k.CreatedUtc,
                    Enabled = k.Enabled,
                    Active = k.Active,
                    Service = k.Service,
                    Data = k.Data,
                    // Decrypt sensitive fields
                    ApiKey = _keyService.Unprotect(k.ApiKey),
                    Secret = _keyService.Unprotect(k.Secret),
                    Passphrase = _keyService.Unprotect(k.Passphrase)
                };
                pd.Keys.Add(copy);
            }

            for (int h=0; h<24; h++) 
            {
                if (!_timeFilterService.IsTradableHour(h)) 
                {
                    pd.BlockedHours.Add(h);
                }
            }

            var json = UtilCompat.JsonSerialize(pd);
            var bytes = Encoding.UTF8.GetBytes(json);
            var enc = Encrypt(bytes, passphrase);
            File.WriteAllBytes(path, enc);
        }

        public void Import(string path, string passphrase)
        {
            Log.Info($"Importing profile from {path}");
            var enc = File.ReadAllBytes(path);
            var plain = Decrypt(enc, passphrase);
            var json = Encoding.UTF8.GetString(plain);
            var pd = UtilCompat.JsonDeserialize<ProfileData>(json);
            if (pd == null) throw new InvalidDataException("invalid profile");

            if (pd.Keys != null)
            {
                foreach (var k in pd.Keys)
                {
                    k.ApiKey = _keyService.Protect(k.ApiKey);
                    k.Secret = _keyService.Protect(k.Secret);
                    k.Passphrase = _keyService.Protect(k.Passphrase);
                }
            }

            _accountService.ReplaceAll(pd.Accounts);
            _keyService.ReplaceAll(pd.Keys);

            _timeFilterService.Clear();
            foreach (var h in pd.BlockedHours) 
            {
                _timeFilterService.BlockHour(h);
            }
        }

        private byte[] Encrypt(byte[] data, string pass)
        {
            var salt = new byte[16]; new RNGCryptoServiceProvider().GetBytes(salt);
            using (var r = new Rfc2898DeriveBytes(pass ?? "", salt, 100000))
            using (var aes = new AesManaged())
            {
                aes.Key = r.GetBytes(32);
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using (var ms = new MemoryStream())
                {
                    var magic = Encoding.ASCII.GetBytes(Magic);
                    ms.Write(magic, 0, magic.Length);
                    ms.Write(salt, 0, salt.Length);
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        private byte[] Decrypt(byte[] enc, string pass)
        {
            using (var ms = new MemoryStream(enc))
            {
                var magic = new byte[Magic.Length];
                if (ms.Read(magic, 0, magic.Length) != magic.Length) throw new InvalidDataException("bad profile");
                if (Encoding.ASCII.GetString(magic) != Magic) throw new InvalidDataException("bad header");
                var salt = new byte[16]; if (ms.Read(salt, 0, 16) != 16) throw new InvalidDataException("bad salt");
                var iv = new byte[16]; if (ms.Read(iv, 0, 16) != 16) throw new InvalidDataException("bad iv");
                using (var r = new Rfc2898DeriveBytes(pass ?? "", salt, 100000))
                using (var aes = new AesManaged())
                {
                    aes.Key = r.GetBytes(32);
                    aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var outMs = new MemoryStream())
                    {
                        cs.CopyTo(outMs);
                        return outMs.ToArray();
                    }
                }
            }
        }
    }
}