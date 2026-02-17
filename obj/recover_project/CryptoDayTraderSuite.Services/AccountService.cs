using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
	public class AccountService : IAccountService
	{
		private readonly object _lock = new object();

		private List<AccountInfo> _items = new List<AccountInfo>();

		private string StorePath
		{
			get
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				return Path.Combine(dir, "accounts.json");
			}
		}

		public AccountService()
		{
			TryLoad();
		}

		private void TryLoad()
		{
			try
			{
				if (File.Exists(StorePath))
				{
					string json = File.ReadAllText(StorePath, Encoding.UTF8);
					List<AccountInfo> list = UtilCompat.JsonDeserialize<List<AccountInfo>>(json) ?? new List<AccountInfo>();
					lock (_lock)
					{
						_items = list;
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("AccountService Load Error (Corruption)", ex, "TryLoad", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AccountService.cs", 52);
				try
				{
					if (File.Exists(StorePath))
					{
						File.Copy(StorePath, StorePath + ".corrupt." + DateTime.UtcNow.Ticks + ".bak", overwrite: true);
					}
				}
				catch
				{
				}
			}
		}

		private void Save()
		{
			try
			{
				List<AccountInfo> snap;
				lock (_lock)
				{
					snap = new List<AccountInfo>(_items);
				}
				string json = UtilCompat.JsonSerialize(snap);
				string path = StorePath;
				string tempPath = path + ".tmp";
				string bakPath = path + ".bak";
				File.WriteAllText(tempPath, json, Encoding.UTF8);
				if (File.Exists(path))
				{
					if (File.Exists(bakPath))
					{
						File.Delete(bakPath);
					}
					File.Replace(tempPath, path, bakPath);
				}
				else
				{
					File.Move(tempPath, path);
				}
			}
			catch (Exception ex)
			{
				Log.Error("AccountService Save Error", ex, "Save", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AccountService.cs", 88);
				throw;
			}
		}

		public List<AccountInfo> GetAll()
		{
			lock (_lock)
			{
				return new List<AccountInfo>(_items);
			}
		}

		public AccountInfo Get(string id)
		{
			lock (_lock)
			{
				return _items.Find((AccountInfo a) => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
			}
		}

		public void Upsert(AccountInfo info)
		{
			if (info == null)
			{
				return;
			}
			lock (_lock)
			{
				int idx = _items.FindIndex((AccountInfo a) => string.Equals(a.Id, info.Id, StringComparison.OrdinalIgnoreCase));
				if (idx >= 0)
				{
					_items[idx] = info;
				}
				else
				{
					_items.Add(info);
				}
			}
			Save();
		}

		public void Delete(string id)
		{
			lock (_lock)
			{
				_items.RemoveAll((AccountInfo a) => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
			}
			Save();
		}

		public void ReplaceAll(List<AccountInfo> items)
		{
			lock (_lock)
			{
				_items = items ?? new List<AccountInfo>();
			}
			Save();
		}
	}
}
