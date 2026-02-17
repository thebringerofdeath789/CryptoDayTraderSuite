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
			Log.Info("Exporting profile to " + path, "Export", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ProfileService.cs", 28);
			ProfileData pd = new ProfileData();
			pd.Accounts = _accountService.GetAll();
			List<KeyInfo> keys = _keyService.GetAll();
			pd.Keys = new List<KeyInfo>();
			foreach (KeyInfo k in keys)
			{
				KeyInfo copy = new KeyInfo
				{
					Broker = k.Broker,
					Label = k.Label,
					CreatedUtc = k.CreatedUtc,
					Enabled = k.Enabled,
					Active = k.Active,
					Service = k.Service,
					Data = k.Data,
					ApiKey = _keyService.Unprotect(k.ApiKey),
					Secret = _keyService.Unprotect(k.Secret),
					Passphrase = _keyService.Unprotect(k.Passphrase)
				};
				pd.Keys.Add(copy);
			}
			for (int h = 0; h < 24; h++)
			{
				if (!_timeFilterService.IsTradableHour(h))
				{
					pd.BlockedHours.Add(h);
				}
			}
			string json = UtilCompat.JsonSerialize(pd);
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			byte[] enc = Encrypt(bytes, passphrase);
			File.WriteAllBytes(path, enc);
		}

		public void Import(string path, string passphrase)
		{
			Log.Info("Importing profile from " + path, "Import", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ProfileService.cs", 69);
			byte[] enc = File.ReadAllBytes(path);
			byte[] plain = Decrypt(enc, passphrase);
			string json = Encoding.UTF8.GetString(plain);
			ProfileData pd = UtilCompat.JsonDeserialize<ProfileData>(json);
			if (pd == null)
			{
				throw new Exception("invalid profile");
			}
			if (pd.Keys != null)
			{
				foreach (KeyInfo k in pd.Keys)
				{
					k.ApiKey = _keyService.Protect(k.ApiKey);
					k.Secret = _keyService.Protect(k.Secret);
					k.Passphrase = _keyService.Protect(k.Passphrase);
				}
			}
			_accountService.ReplaceAll(pd.Accounts);
			_keyService.ReplaceAll(pd.Keys);
			_timeFilterService.Clear();
			foreach (int h in pd.BlockedHours)
			{
				_timeFilterService.BlockHour(h);
			}
		}

		private byte[] Encrypt(byte[] data, string pass)
		{
			byte[] salt = new byte[16];
			new RNGCryptoServiceProvider().GetBytes(salt);
			using (Rfc2898DeriveBytes r = new Rfc2898DeriveBytes(pass ?? "", salt, 100000))
			{
				using (AesManaged aes = new AesManaged())
				{
					aes.Key = r.GetBytes(32);
					aes.GenerateIV();
					aes.Mode = CipherMode.CBC;
					aes.Padding = PaddingMode.PKCS7;
					using (MemoryStream ms = new MemoryStream())
					{
						byte[] magic = Encoding.ASCII.GetBytes("CDTPv1\0");
						ms.Write(magic, 0, magic.Length);
						ms.Write(salt, 0, salt.Length);
						ms.Write(aes.IV, 0, aes.IV.Length);
						using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
						{
							cs.Write(data, 0, data.Length);
							cs.FlushFinalBlock();
						}
						return ms.ToArray();
					}
				}
			}
		}

		private byte[] Decrypt(byte[] enc, string pass)
		{
			using (MemoryStream ms = new MemoryStream(enc))
			{
				byte[] magic = new byte["CDTPv1\0".Length];
				if (ms.Read(magic, 0, magic.Length) != magic.Length)
				{
					throw new Exception("bad profile");
				}
				if (Encoding.ASCII.GetString(magic) != "CDTPv1\0")
				{
					throw new Exception("bad header");
				}
				byte[] salt = new byte[16];
				if (ms.Read(salt, 0, 16) != 16)
				{
					throw new Exception("bad salt");
				}
				byte[] iv = new byte[16];
				if (ms.Read(iv, 0, 16) != 16)
				{
					throw new Exception("bad iv");
				}
				using (Rfc2898DeriveBytes r = new Rfc2898DeriveBytes(pass ?? "", salt, 100000))
				{
					using (AesManaged aes = new AesManaged())
					{
						aes.Key = r.GetBytes(32);
						aes.IV = iv;
						aes.Mode = CipherMode.CBC;
						aes.Padding = PaddingMode.PKCS7;
						using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
						{
							using (MemoryStream outMs = new MemoryStream())
							{
								cs.CopyTo(outMs);
								return outMs.ToArray();
							}
						}
					}
				}
			}
		}
	}
}
