using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public interface IAccountService
    {
        List<AccountInfo> GetAll();
        AccountInfo Get(string id);
        void Upsert(AccountInfo info);
        void Delete(string id);
        void ReplaceAll(List<AccountInfo> items);
    }

    public class AccountService : IAccountService
    {
        private readonly object _lock = new object();
        private List<AccountInfo> _items = new List<AccountInfo>();
        
        private string StorePath
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
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
                    var json = File.ReadAllText(StorePath, Encoding.UTF8);
                    var list = UtilCompat.JsonDeserialize<List<AccountInfo>>(json) ?? new List<AccountInfo>();
                    lock (_lock) _items = list;
                }
            }
            catch (Exception ex)
            {
                Log.Error("AccountService Load Error (Corruption)", ex);
                try 
                {
                    if (File.Exists(StorePath))
                        File.Copy(StorePath, StorePath + ".corrupt." + DateTime.UtcNow.Ticks + ".bak", true);
                } 
                catch { }
            }
        }

        private void Save()
        {
            try
            {
                List<AccountInfo> snap;
                lock (_lock) snap = new List<AccountInfo>(_items);
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
            catch (Exception ex)
            {
                Log.Error("AccountService Save Error", ex);
                throw;
            }
        }

        public List<AccountInfo> GetAll()
        {
            lock (_lock) return new List<AccountInfo>(_items);
        }

        public AccountInfo Get(string id)
        {
            lock (_lock) return _items.Find(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public void Upsert(AccountInfo info)
        {
            if (info == null) return;
            lock (_lock)
            {
                var idx = _items.FindIndex(a => string.Equals(a.Id, info.Id, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) _items[idx] = info; else _items.Add(info);
            }
            Save();
        }

        public void Delete(string id)
        {
            lock (_lock) _items.RemoveAll(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
            Save();
        }
        
        public void ReplaceAll(List<AccountInfo> items)
        {
            lock (_lock) _items = items ?? new List<AccountInfo>();
            Save();
        }
    }
}
