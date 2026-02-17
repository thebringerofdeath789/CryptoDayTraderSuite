using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
	public class AutoModeProfileService : IAutoModeProfileService
	{
		private const int CurrentStoreVersion = 2;

		private readonly object _lock = new object();

		private List<AutoModeProfile> _profiles = new List<AutoModeProfile>();

		private string StorePath
		{
			get
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
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
			if (string.IsNullOrWhiteSpace(profileId))
			{
				return null;
			}
			lock (_lock)
			{
				AutoModeProfile found = _profiles.FirstOrDefault((AutoModeProfile p) => string.Equals(p.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
				return (found == null) ? null : Clone(found);
			}
		}

		public void Upsert(AutoModeProfile profile)
		{
			if (profile == null)
			{
				throw new ArgumentNullException("profile");
			}
			lock (_lock)
			{
				AutoModeProfile normalized = Normalize(profile);
				int idx = _profiles.FindIndex((AutoModeProfile p) => string.Equals(p.ProfileId, normalized.ProfileId, StringComparison.OrdinalIgnoreCase));
				if (idx >= 0)
				{
					_profiles[idx] = normalized;
				}
				else
				{
					_profiles.Add(normalized);
				}
			}
			Save();
		}

		public void Delete(string profileId)
		{
			if (string.IsNullOrWhiteSpace(profileId))
			{
				return;
			}
			lock (_lock)
			{
				_profiles.RemoveAll((AutoModeProfile p) => string.Equals(p.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
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
			bool shouldRewriteStore = false;
			try
			{
				if (!File.Exists(StorePath))
				{
					return;
				}
				string json = File.ReadAllText(StorePath, Encoding.UTF8);
				if (string.IsNullOrWhiteSpace(json))
				{
					return;
				}
				AutoModeProfileStore store = UtilCompat.JsonDeserialize<AutoModeProfileStore>(json);
				if (store != null && store.Profiles != null)
				{
					AutoModeProfileStore migratedStore = MigrateStore(store, out shouldRewriteStore);
					lock (_lock)
					{
						_profiles = migratedStore.Profiles.Select(Normalize).ToList();
					}
					if (shouldRewriteStore)
					{
						Save();
					}
				}
				else
				{
					List<AutoModeProfile> legacy = UtilCompat.JsonDeserialize<List<AutoModeProfile>>(json);
					lock (_lock)
					{
						_profiles = (legacy ?? new List<AutoModeProfile>()).Select(Normalize).ToList();
					}
					Save();
				}
			}
			catch (Exception ex)
			{
				Log.Error("AutoModeProfileService Load Error", ex, "TryLoad", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoModeProfileService.cs", 133);
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
					AutoModeProfileStore autoModeProfileStore = new AutoModeProfileStore();
					autoModeProfileStore.Version = 2;
					autoModeProfileStore.Profiles = _profiles.Select(Clone).ToList();
					payload = autoModeProfileStore;
				}
				string json = UtilCompat.JsonSerialize(payload);
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
				Log.Error("AutoModeProfileService Save Error", ex, "Save", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoModeProfileService.cs", 181);
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
					Version = 2,
					Profiles = new List<AutoModeProfile>()
				};
			}
			int sourceVersion = ((store.Version <= 0) ? 1 : store.Version);
			AutoModeProfileStore autoModeProfileStore = new AutoModeProfileStore();
			autoModeProfileStore.Version = 2;
			autoModeProfileStore.Profiles = (store.Profiles ?? new List<AutoModeProfile>()).Select(Clone).ToList();
			AutoModeProfileStore migrated = autoModeProfileStore;
			if (sourceVersion < 2)
			{
				changed = true;
				foreach (AutoModeProfile profile in migrated.Profiles)
				{
					if (profile != null)
					{
						if (string.IsNullOrWhiteSpace(profile.PairScope))
						{
							profile.PairScope = "Selected";
						}
						if (profile.IntervalMinutes < 1)
						{
							profile.IntervalMinutes = 5;
						}
						if (profile.MaxTradesPerCycle < 1)
						{
							profile.MaxTradesPerCycle = 3;
						}
						if (profile.CooldownMinutes < 1)
						{
							profile.CooldownMinutes = 30;
						}
						if (profile.DailyRiskStopPct <= 0m)
						{
							profile.DailyRiskStopPct = 3m;
						}
						if (profile.CreatedUtc == default(DateTime))
						{
							profile.CreatedUtc = DateTime.UtcNow;
						}
					}
				}
				Log.Info("[AutoModeProfileService] Migrated profile store v" + sourceVersion + " -> v" + 2 + ".", "MigrateStore", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoModeProfileService.cs", 223);
			}
			return migrated;
		}

		private static AutoModeProfile Normalize(AutoModeProfile profile)
		{
			DateTime now = DateTime.UtcNow;
			AutoModeProfile p = Clone(profile) ?? new AutoModeProfile();
			if (string.IsNullOrWhiteSpace(p.ProfileId))
			{
				p.ProfileId = Guid.NewGuid().ToString("N");
			}
			if (string.IsNullOrWhiteSpace(p.Name))
			{
				p.Name = "Auto Profile " + now.ToString("yyyyMMdd_HHmmss");
			}
			if (string.IsNullOrWhiteSpace(p.PairScope))
			{
				p.PairScope = "Selected";
			}
			p.PairScope = (string.Equals(p.PairScope, "All", StringComparison.OrdinalIgnoreCase) ? "All" : "Selected");
			p.SelectedPairs = (from s in p.SelectedPairs ?? new List<string>()
				where !string.IsNullOrWhiteSpace(s)
				select s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (p.IntervalMinutes < 1)
			{
				p.IntervalMinutes = 1;
			}
			if (p.MaxTradesPerCycle < 1)
			{
				p.MaxTradesPerCycle = 1;
			}
			if (p.CooldownMinutes < 1)
			{
				p.CooldownMinutes = 1;
			}
			if (p.DailyRiskStopPct < 0.1m)
			{
				p.DailyRiskStopPct = 0.1m;
			}
			if (p.DailyRiskStopPct > 100m)
			{
				p.DailyRiskStopPct = 100m;
			}
			if (p.CreatedUtc == default(DateTime))
			{
				p.CreatedUtc = now;
			}
			p.UpdatedUtc = now;
			return p;
		}

		private static AutoModeProfile Clone(AutoModeProfile p)
		{
			if (p == null)
			{
				return null;
			}
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
