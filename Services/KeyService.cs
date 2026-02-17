using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
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
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
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
                    var json = File.ReadAllText(StorePath, Encoding.UTF8);
                    var list = UtilCompat.JsonDeserialize<List<KeyInfo>>(json) ?? new List<KeyInfo>();
                    lock (_lock) _items = list;
                }
            }
            catch (Exception ex)
            {
                Log.Error("KeyService Load Error", ex);
                // Backup corrupt file
                try { File.Copy(StorePath, StorePath + ".corrupt." + DateTime.UtcNow.Ticks + ".bak", true); } catch { }
            }
        }

        private void TryLoadActive()
        {
            try
            {
                if (File.Exists(ActivePath))
                {
                    var json = File.ReadAllText(ActivePath, Encoding.UTF8);
                    var map = UtilCompat.JsonDeserialize<Dictionary<string, string>>(json);
                    _activeByBroker = map ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { _activeByBroker = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
        }

        private void Save()
        {
            try
            {
                List<KeyInfo> snap;
                lock (_lock) snap = new List<KeyInfo>(_items);
                var json = UtilCompat.JsonSerialize(snap);
                
                var path = StorePath;
                var tempPath = path + ".tmp";
                var bakPath = path + ".bak";

                File.WriteAllText(tempPath, json, Encoding.UTF8);
                if (File.Exists(path))
                {
                    if (File.Exists(bakPath)) File.Delete(bakPath);
                    File.Replace(tempPath, path, bakPath);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (Exception ex) { Log.Error("KeyService Save Error", ex); }
        }

        private void SaveActive()
        {
            try { File.WriteAllText(ActivePath, UtilCompat.JsonSerialize(_activeByBroker), Encoding.UTF8); }
            catch { }
        }

        public List<KeyInfo> GetAll() { lock (_lock) return new List<KeyInfo>(_items); }

        public void ReplaceAll(List<KeyInfo> items)
        {
            lock (_lock) _items = items ?? new List<KeyInfo>();
            Save();
        }

        public void Upsert(KeyInfo info)
        {
            if (info == null) return;
            lock (_lock)
            {
                var idx = _items.FindIndex(k =>
                    IsSameKeyPart(k.Broker, info.Broker) &&
                    IsSameKeyPart(k.Label, info.Label));
                if (idx >= 0) _items[idx] = info; else _items.Add(info);
            }
            Save();
        }

        public void Remove(string broker, string label)
        {
            var normalizedBroker = NormalizeKeyPart(broker);
            var normalizedLabel = NormalizeKeyPart(label);
            lock (_lock)
            {
                _items.RemoveAll(k =>
                    IsSameKeyPart(k.Broker, normalizedBroker) &&
                    IsSameKeyPart(k.Label, normalizedLabel));

                var removeIds = new List<string>();
                foreach (var pair in _activeByBroker)
                {
                    string activeBroker;
                    string activeLabel;
                    KeyEntry.SplitId(pair.Value, out activeBroker, out activeLabel);
                    if (IsSameKeyPart(activeBroker, normalizedBroker) && IsSameKeyPart(activeLabel, normalizedLabel))
                    {
                        removeIds.Add(pair.Key);
                    }
                }

                foreach (var activeBrokerKey in removeIds)
                {
                    _activeByBroker.Remove(activeBrokerKey);
                }
            }
            Save();
            SaveActive();
        }

        public void Delete(string id)
        {
            string broker, label; KeyEntry.SplitId(id, out broker, out label);
            Remove(broker, label);
        }

        public KeyInfo Get(string id)
        {
            string broker, label; KeyEntry.SplitId(id, out broker, out label);
            return Get(broker, label);
        }

        public KeyInfo Get(string broker, string label)
        {
            var normalizedBroker = NormalizeKeyPart(broker);
            var normalizedLabel = NormalizeKeyPart(label);
            lock (_lock)
            {
                return _items.Find(k =>
                    IsSameKeyPart(k.Broker, normalizedBroker) &&
                    IsSameKeyPart(k.Label, normalizedLabel));
            }
        }

        public void SetActive(string id)
        {
            string broker, label; KeyEntry.SplitId(id, out broker, out label);
            if (string.IsNullOrEmpty(broker)) return;
            _activeByBroker[broker] = id;
            SaveActive();
        }

        public void SetActive(string broker, string label) => SetActive(KeyEntry.MakeId(broker, label));

        public string GetActiveId()
        {
            foreach (var kv in _activeByBroker) if (!string.IsNullOrWhiteSpace(kv.Value)) return kv.Value;
            lock (_lock)
            {
                var any = _items.Find(k => k.Enabled);
                return any != null ? KeyEntry.MakeId(any.Broker, any.Label) : null;
            }
        }

        public string GetActiveId(string broker)
        {
            if (string.IsNullOrEmpty(broker)) return GetActiveId();
            string id;
            if (_activeByBroker.TryGetValue(broker, out id) && !string.IsNullOrWhiteSpace(id))
            {
                var key = Get(id);
                if (key != null) return id;

                var repaired = RepairActiveId(broker, id);
                if (!string.IsNullOrEmpty(repaired))
                {
                    _activeByBroker[broker] = repaired;
                    SaveActive();
                    return repaired;
                }
            }
            lock (_lock)
            {
                var any = _items.Find(k => string.Equals(k.Broker, broker, StringComparison.OrdinalIgnoreCase) && k.Enabled);
                return any != null ? KeyEntry.MakeId(any.Broker, any.Label) : null;
            }
        }

        private string RepairActiveId(string broker, string id)
        {
            string idBroker, idLabel;
            KeyEntry.SplitId(id, out idBroker, out idLabel);

            var normalizedBroker = NormalizeKeyPart(broker);
            var normalizedLabel = NormalizeKeyPart(idLabel);

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(normalizedLabel))
                {
                    var exact = _items.Find(k =>
                        IsSameKeyPart(k.Broker, normalizedBroker) &&
                        IsSameKeyPart(k.Label, normalizedLabel) && k.Enabled);
                    if (exact != null) return KeyEntry.MakeId(exact.Broker, exact.Label);
                }

                var fallback = _items.Find(k => IsSameKeyPart(k.Broker, normalizedBroker) && k.Enabled);
                return fallback != null ? KeyEntry.MakeId(fallback.Broker, fallback.Label) : null;
            }
        }

        private static string NormalizeKeyPart(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsSameKeyPart(string left, string right)
        {
            return string.Equals(NormalizeKeyPart(left), NormalizeKeyPart(right), StringComparison.OrdinalIgnoreCase);
        }

        public string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return plain;
            try {
                var bytes = Encoding.UTF8.GetBytes(plain);
                var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(enc);
            } catch { return plain; }
        }

        public string Unprotect(string cipher)
        {
             if (string.IsNullOrEmpty(cipher)) return cipher;
             try {
                var enc = Convert.FromBase64String(cipher);
                var dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(dec);
             } catch { return String.Empty; }
        }
    }
}
