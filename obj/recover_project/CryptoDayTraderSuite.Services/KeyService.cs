using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
	public class KeyService : IKeyService
	{
		private readonly object _lock = new object();

		private List<KeyInfo> _items = new List<KeyInfo>();

		private Dictionary<string, string> _activeByBroker = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private string BaseDir
		{
			get
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				return dir;
			}
		}

		private string StorePath => Path.Combine(BaseDir, "keys.json");

		private string ActivePath => Path.Combine(BaseDir, "keys.active.json");

		public KeyService()
		{
			TryLoad();
			TryLoadActive();
		}

		private void TryLoad()
		{
			try
			{
				if (File.Exists(StorePath))
				{
					string json = File.ReadAllText(StorePath, Encoding.UTF8);
					List<KeyInfo> list = UtilCompat.JsonDeserialize<List<KeyInfo>>(json) ?? new List<KeyInfo>();
					lock (_lock)
					{
						_items = list;
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("KeyService Load Error", ex, "TryLoad", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\KeyService.cs", 48);
				try
				{
					File.Copy(StorePath, StorePath + ".corrupt." + DateTime.UtcNow.Ticks + ".bak", overwrite: true);
				}
				catch
				{
				}
			}
		}

		private void TryLoadActive()
		{
			try
			{
				if (File.Exists(ActivePath))
				{
					string json = File.ReadAllText(ActivePath, Encoding.UTF8);
					Dictionary<string, string> map = UtilCompat.JsonDeserialize<Dictionary<string, string>>(json);
					_activeByBroker = map ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
			}
			catch
			{
				_activeByBroker = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
		}

		private void Save()
		{
			try
			{
				List<KeyInfo> snap;
				lock (_lock)
				{
					snap = new List<KeyInfo>(_items);
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
				Log.Error("KeyService Save Error", ex, "Save", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\KeyService.cs", 91);
			}
		}

		private void SaveActive()
		{
			try
			{
				File.WriteAllText(ActivePath, UtilCompat.JsonSerialize(_activeByBroker), Encoding.UTF8);
			}
			catch
			{
			}
		}

		public List<KeyInfo> GetAll()
		{
			lock (_lock)
			{
				return new List<KeyInfo>(_items);
			}
		}

		public void ReplaceAll(List<KeyInfo> items)
		{
			lock (_lock)
			{
				_items = items ?? new List<KeyInfo>();
			}
			Save();
		}

		public void Upsert(KeyInfo info)
		{
			if (info == null)
			{
				return;
			}
			lock (_lock)
			{
				int idx = _items.FindIndex((KeyInfo k) => string.Equals(k.Broker, info.Broker, StringComparison.OrdinalIgnoreCase) && string.Equals(k.Label, info.Label, StringComparison.OrdinalIgnoreCase));
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

		public void Remove(string broker, string label)
		{
			lock (_lock)
			{
				_items.RemoveAll((KeyInfo k) => string.Equals(k.Broker, broker, StringComparison.OrdinalIgnoreCase) && string.Equals(k.Label, label, StringComparison.OrdinalIgnoreCase));
			}
			Save();
		}

		public void Delete(string id)
		{
			KeyEntry.SplitId(id, out var broker, out var label);
			Remove(broker, label);
		}

		public KeyInfo Get(string id)
		{
			KeyEntry.SplitId(id, out var broker, out var label);
			return Get(broker, label);
		}

		public KeyInfo Get(string broker, string label)
		{
			lock (_lock)
			{
				return _items.Find((KeyInfo k) => string.Equals(k.Broker, broker, StringComparison.OrdinalIgnoreCase) && string.Equals(k.Label, label, StringComparison.OrdinalIgnoreCase));
			}
		}

		public void SetActive(string id)
		{
			KeyEntry.SplitId(id, out var broker, out var _);
			if (!string.IsNullOrEmpty(broker))
			{
				_activeByBroker[broker] = id;
				SaveActive();
			}
		}

		public void SetActive(string broker, string label)
		{
			SetActive(KeyEntry.MakeId(broker, label));
		}

		public string GetActiveId()
		{
			foreach (KeyValuePair<string, string> kv in _activeByBroker)
			{
				if (!string.IsNullOrWhiteSpace(kv.Value))
				{
					return kv.Value;
				}
			}
			lock (_lock)
			{
				KeyInfo any = _items.Find((KeyInfo k) => k.Enabled);
				return (any != null) ? KeyEntry.MakeId(any.Broker, any.Label) : null;
			}
		}

		public string GetActiveId(string broker)
		{
			if (string.IsNullOrEmpty(broker))
			{
				return GetActiveId();
			}
			if (_activeByBroker.TryGetValue(broker, out var id) && !string.IsNullOrWhiteSpace(id))
			{
				KeyInfo key = Get(id);
				if (key != null)
				{
					return id;
				}
				string repaired = RepairActiveId(broker, id);
				if (!string.IsNullOrEmpty(repaired))
				{
					_activeByBroker[broker] = repaired;
					SaveActive();
					return repaired;
				}
			}
			lock (_lock)
			{
				KeyInfo any = _items.Find((KeyInfo k) => string.Equals(k.Broker, broker, StringComparison.OrdinalIgnoreCase) && k.Enabled);
				return (any != null) ? KeyEntry.MakeId(any.Broker, any.Label) : null;
			}
		}

		private string RepairActiveId(string broker, string id)
		{
			KeyEntry.SplitId(id, out var _, out var idLabel);
			lock (_lock)
			{
				if (!string.IsNullOrEmpty(idLabel))
				{
					KeyInfo exact = _items.Find((KeyInfo k) => string.Equals(k.Broker, broker, StringComparison.OrdinalIgnoreCase) && string.Equals(k.Label, idLabel, StringComparison.OrdinalIgnoreCase) && k.Enabled);
					if (exact != null)
					{
						return KeyEntry.MakeId(exact.Broker, exact.Label);
					}
				}
				KeyInfo fallback = _items.Find((KeyInfo k) => string.Equals(k.Broker, broker, StringComparison.OrdinalIgnoreCase) && k.Enabled);
				return (fallback != null) ? KeyEntry.MakeId(fallback.Broker, fallback.Label) : null;
			}
		}

		public string Protect(string plain)
		{
			if (string.IsNullOrEmpty(plain))
			{
				return plain;
			}
			try
			{
				byte[] bytes = Encoding.UTF8.GetBytes(plain);
				byte[] enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
				return Convert.ToBase64String(enc);
			}
			catch
			{
				return plain;
			}
		}

		public string Unprotect(string cipher)
		{
			if (string.IsNullOrEmpty(cipher))
			{
				return cipher;
			}
			try
			{
				byte[] enc = Convert.FromBase64String(cipher);
				byte[] dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
				return Encoding.UTF8.GetString(dec);
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
