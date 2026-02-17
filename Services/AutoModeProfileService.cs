using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public interface IAutoModeProfileService
    {
        List<AutoModeProfile> GetAll();
        AutoModeProfile Get(string profileId);
        void Upsert(AutoModeProfile profile);
        void Delete(string profileId);
        void ReplaceAll(List<AutoModeProfile> profiles);
    }

    public class AutoModeProfileService : IAutoModeProfileService
    {
        private const int CurrentStoreVersion = 2;
        private readonly object _lock = new object();
        private List<AutoModeProfile> _profiles = new List<AutoModeProfile>();

        private string StorePath
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "automode_profiles.json");
            }
        }

        public AutoModeProfileService()
        {
            TryLoad();
        }

        public List<AutoModeProfile> GetAll()
        {
            lock (_lock)
            {
                return _profiles.Select(Clone).ToList();
            }
        }

        public AutoModeProfile Get(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId)) return null;
            lock (_lock)
            {
                var found = _profiles.FirstOrDefault(p => string.Equals(p.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
                return found == null ? null : Clone(found);
            }
        }

        public void Upsert(AutoModeProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            lock (_lock)
            {
                var normalized = Normalize(profile);
                var idx = _profiles.FindIndex(p => string.Equals(p.ProfileId, normalized.ProfileId, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) _profiles[idx] = normalized;
                else _profiles.Add(normalized);
            }

            Save();
        }

        public void Delete(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId)) return;
            lock (_lock)
            {
                _profiles.RemoveAll(p => string.Equals(p.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
            }

            Save();
        }

        public void ReplaceAll(List<AutoModeProfile> profiles)
        {
            lock (_lock)
            {
                _profiles = (profiles ?? new List<AutoModeProfile>()).Select(Normalize).ToList();
            }

            Save();
        }

        private void TryLoad()
        {
            var shouldRewriteStore = false;

            try
            {
                if (!File.Exists(StorePath)) return;

                var json = File.ReadAllText(StorePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return;

                var store = UtilCompat.JsonDeserialize<AutoModeProfileStore>(json);
                if (store != null && store.Profiles != null)
                {
                    var migratedStore = MigrateStore(store, out shouldRewriteStore);
                    lock (_lock)
                    {
                        _profiles = migratedStore.Profiles.Select(Normalize).ToList();
                    }

                    if (shouldRewriteStore)
                    {
                        Save();
                    }

                    return;
                }

                var legacy = UtilCompat.JsonDeserialize<List<AutoModeProfile>>(json);
                lock (_lock)
                {
                    _profiles = (legacy ?? new List<AutoModeProfile>()).Select(Normalize).ToList();
                }

                Save();
            }
            catch (Exception ex)
            {
                Log.Error("AutoModeProfileService Load Error", ex);
                try
                {
                    if (File.Exists(StorePath))
                    {
                        File.Copy(StorePath, StorePath + ".corrupt." + DateTime.UtcNow.Ticks + ".bak", true);
                    }
                }
                catch { }
                lock (_lock)
                {
                    _profiles = new List<AutoModeProfile>();
                }
            }
        }

        private void Save()
        {
            try
            {
                AutoModeProfileStore payload;
                lock (_lock)
                {
                    payload = new AutoModeProfileStore
                    {
                        Version = CurrentStoreVersion,
                        Profiles = _profiles.Select(Clone).ToList()
                    };
                }

                var json = UtilCompat.JsonSerialize(payload);
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
                Log.Error("AutoModeProfileService Save Error", ex);
                throw;
            }
        }

        private static AutoModeProfileStore MigrateStore(AutoModeProfileStore store, out bool changed)
        {
            changed = false;

            if (store == null)
            {
                changed = true;
                return new AutoModeProfileStore
                {
                    Version = CurrentStoreVersion,
                    Profiles = new List<AutoModeProfile>()
                };
            }

            var sourceVersion = store.Version <= 0 ? 1 : store.Version;
            var migrated = new AutoModeProfileStore
            {
                Version = CurrentStoreVersion,
                Profiles = (store.Profiles ?? new List<AutoModeProfile>()).Select(Clone).ToList()
            };

            if (sourceVersion < CurrentStoreVersion)
            {
                changed = true;

                foreach (var profile in migrated.Profiles)
                {
                    if (profile == null) continue;

                    if (string.IsNullOrWhiteSpace(profile.PairScope)) profile.PairScope = "Selected";
                    if (profile.IntervalMinutes < 1) profile.IntervalMinutes = 5;
                    if (profile.MaxTradesPerCycle < 1) profile.MaxTradesPerCycle = 3;
                    if (profile.CooldownMinutes < 1) profile.CooldownMinutes = 30;
                    if (profile.DailyRiskStopPct <= 0m) profile.DailyRiskStopPct = 3m;
                    if (profile.CreatedUtc == default(DateTime)) profile.CreatedUtc = DateTime.UtcNow;
                }

                Log.Info("[AutoModeProfileService] Migrated profile store v" + sourceVersion + " -> v" + CurrentStoreVersion + ".");
            }

            return migrated;
        }

        private static AutoModeProfile Normalize(AutoModeProfile profile)
        {
            var now = DateTime.UtcNow;
            var p = Clone(profile) ?? new AutoModeProfile();

            if (string.IsNullOrWhiteSpace(p.ProfileId)) p.ProfileId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(p.Name)) p.Name = "Auto Profile " + now.ToString("yyyyMMdd_HHmmss");
            if (string.IsNullOrWhiteSpace(p.PairScope)) p.PairScope = "Selected";

            p.PairScope = string.Equals(p.PairScope, "All", StringComparison.OrdinalIgnoreCase) ? "All" : "Selected";
            p.SelectedPairs = (p.SelectedPairs ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (p.IntervalMinutes < 1) p.IntervalMinutes = 1;
            if (p.MaxTradesPerCycle < 1) p.MaxTradesPerCycle = 1;
            if (p.CooldownMinutes < 1) p.CooldownMinutes = 1;
            if (p.DailyRiskStopPct < 0.1m) p.DailyRiskStopPct = 0.1m;
            if (p.DailyRiskStopPct > 100m) p.DailyRiskStopPct = 100m;

            if (p.CreatedUtc == default(DateTime)) p.CreatedUtc = now;
            p.UpdatedUtc = now;

            return p;
        }

        private static AutoModeProfile Clone(AutoModeProfile p)
        {
            if (p == null) return null;
            return new AutoModeProfile
            {
                ProfileId = p.ProfileId,
                Name = p.Name,
                AccountId = p.AccountId,
                Enabled = p.Enabled,
                PairScope = p.PairScope,
                SelectedPairs = (p.SelectedPairs ?? new List<string>()).ToList(),
                IntervalMinutes = p.IntervalMinutes,
                MaxTradesPerCycle = p.MaxTradesPerCycle,
                CooldownMinutes = p.CooldownMinutes,
                DailyRiskStopPct = p.DailyRiskStopPct,
                CreatedUtc = p.CreatedUtc,
                UpdatedUtc = p.UpdatedUtc
            };
        }
    }
}
