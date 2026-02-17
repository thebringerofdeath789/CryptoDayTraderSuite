using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class AccountInfo
	{
		public string Id;                        /* unique id */
		public string Broker;                    /* e.g., Coinbase, Kraken */
		public string DisplayName;               /* friendly name for UI */
		public string DefaultQuote;              /* e.g., USD */
		public bool Paper;                       /* legacy flag */
		public decimal MaxOrderPct;              /* % equity per order */

		public DateTime CreatedUtc = DateTime.UtcNow; /* used by UI */
		public bool Enabled = true;              /* allow this account for auto mode */
		public DateTime UpdatedUtc = DateTime.UtcNow; // <-- Add this line

		/* unified mode as enum to satisfy comparisons like (acc.Mode == AccountMode.Paper) */
		public AccountMode Mode = AccountMode.Live;

		public decimal RiskPerTradePct = 1m;     /* risk per trade in % */
		public int MaxConcurrentTrades = 3;      /* cap concurrent trades */
		public string KeyEntryId;                /* selected key id: Broker|Label */

		/* aliases expected by some UI bits */
		public string Label                     /* alias for DisplayName */
		{
			get { return DisplayName; }
			set { DisplayName = value; }
		}

		public string Service                   /* alias for Broker */
		{
			get { return Broker; }
			set { Broker = value; }
		}
	}

	/* UI profile shape (stringy) with implicit conversions to/from AccountInfo */
	[Serializable]
	public class AccountProfile
	{
		public string Id { get; set; }
		public string Label { get; set; }
		public string Service { get; set; }
		public string Mode { get; set; }                      /* "Live"/"Paper" */
		public string DefaultQuote { get; set; }
		public bool Paper { get; set; }
		public decimal MaxOrderPct { get; set; }
		public decimal RiskPerTradePct { get; set; }
		public int MaxConcurrentTrades { get; set; }
		public string KeyEntryId { get; set; }
		public DateTime CreatedUtc { get; set; }              /* UI reads this */
		public bool Enabled { get; set; }
		public DateTime UpdatedUtc { get; set; }

		public static implicit operator AccountProfile(AccountInfo a)
		{
			if (a == null) return null;
			return new AccountProfile
			{
				Id = a.Id,
				Label = a.Label,
				Service = a.Service,
				Mode = a.Mode.ToString(),        /* enum -> string */
				DefaultQuote = a.DefaultQuote,
				Paper = a.Paper,
				MaxOrderPct = a.MaxOrderPct,
				RiskPerTradePct = a.RiskPerTradePct,
				MaxConcurrentTrades = a.MaxConcurrentTrades,
				KeyEntryId = a.KeyEntryId,
				CreatedUtc = a.CreatedUtc,
				Enabled = a.Enabled,
				UpdatedUtc = a.UpdatedUtc
			};
		}

		public static implicit operator AccountInfo(AccountProfile p)
		{
			if (p == null) return null;
			var info = new AccountInfo
			{
				Id = p.Id,
				DisplayName = p.Label,
				Broker = p.Service,
				DefaultQuote = p.DefaultQuote,
				Paper = p.Paper,
				MaxOrderPct = p.MaxOrderPct,
				RiskPerTradePct = p.RiskPerTradePct,
				MaxConcurrentTrades = p.MaxConcurrentTrades,
				KeyEntryId = p.KeyEntryId,
				CreatedUtc = p.CreatedUtc == default(DateTime) ? DateTime.UtcNow : p.CreatedUtc,
				Enabled = p.Enabled,
				UpdatedUtc = p.UpdatedUtc
			};
			/* map string -> enum safely */
			AccountMode m;
			info.Mode = Enum.TryParse(p.Mode ?? "Live", true, out m) ? m : AccountMode.Live;
			return info;
		}
	}

	[Serializable]
	public class AutoModeProfile
	{
		public string ProfileId;
		public string Name;
		public string AccountId;
		public bool Enabled = true;
		public string PairScope = "Selected"; /* Selected | All */
		public List<string> SelectedPairs = new List<string>();
		public int IntervalMinutes = 5;
		public int MaxTradesPerCycle = 3;
		public int CooldownMinutes = 30;
		public decimal DailyRiskStopPct = 3m;
		public DateTime CreatedUtc = DateTime.UtcNow;
		public DateTime UpdatedUtc = DateTime.UtcNow;
	}

	[Serializable]
	public class AutoModeProfileStore
	{
		public int Version = 1;
		public List<AutoModeProfile> Profiles = new List<AutoModeProfile>();
	}

	/* helper payload for key secrets; some UI uses KeyInfo.Data.TryGetValue(...) */
	[Serializable]
	public class KeyData
	{
		public string ApiKey;
		public string Secret;
		public string Passphrase;
		public string ApiKeyName;
		public string ECPrivateKeyPem;
		public string ApiSecretBase64;

		/* normalize any alias to a canonical field name */
		private static string Canon(string key)
		{
			if (string.IsNullOrWhiteSpace(key)) return "";
			var k = key.Trim().ToLowerInvariant();
			if (k == "apikey" || k == "api_key" || k == "key") return "apikey";
			if (k == "secret" || k == "apisecret" || k == "api_secret" || k == "privatekey" || k == "private_key") return "secret";
			if (k == "passphrase" || k == "phrase" || k == "pass") return "passphrase";
			if (k == "apikeyname") return "apikeyname";
			if (k == "ecprivatekeypem") return "ecprivatekeypem";
			if (k == "apisecretbase64") return "apisecretbase64";
			return k;
		}

		/* map-like accessor used by existing UI code */
		public bool TryGetValue(string key, out string value)
		{
			value = null;
			switch (Canon(key))
			{
				case "apikey": value = ApiKey; break;
				case "secret": value = Secret; break;
				case "passphrase": value = Passphrase; break;
				case "apikeyname": value = ApiKeyName; break;
				case "ecprivatekeypem": value = ECPrivateKeyPem; break;
				case "apisecretbase64": value = ApiSecretBase64; break;
				default: return false;
			}
			return !string.IsNullOrEmpty(value);
		}

		/* requested by CoinbaseExchangeBroker.cs */
		public bool ContainsKey(string key)
		{
			string v; return TryGetValue(key, out v);
		}

		/* indexer so code can do data["apiKey"] */
		public string this[string key]
		{
			get
			{
				string v; TryGetValue(key, out v); return v;
			}
			set
			{
				switch (Canon(key))
				{
					case "apikey": ApiKey = value; break;
					case "secret": Secret = value; break;
					case "passphrase": Passphrase = value; break;
					case "apikeyname": ApiKeyName = value; break;
					case "ecprivatekeypem": ECPrivateKeyPem = value; break;
					case "apisecretbase64": ApiSecretBase64 = value; break;
					default: /* ignore unknown keys */ break;
				}
			}
		}
	}


	[Serializable]
	public class KeyInfo
	{
		public string Broker;
		public string Label;
		public string ApiKey;
		public string Secret;
		public string ApiSecretBase64;
		public string Passphrase;
		public string ApiKeyName;
		public string ECPrivateKeyPem;
		public DateTime CreatedUtc = DateTime.UtcNow;
		public bool Enabled = true;

		/* aliases expected by UI */
		public string Service
		{
			get { return Broker; }
			set { Broker = value; }
		}

		public bool Active;                      /* UI toggles this */

		/* facade so code like key.Data["apiKey"] works (via TryGetValue above) */
		public KeyData Data
		{
			get
			{
				return new KeyData
				{
					ApiKey = ApiKey,
					Secret = Secret,
					Passphrase = Passphrase,
					ApiSecretBase64 = !string.IsNullOrEmpty(ApiSecretBase64) ? ApiSecretBase64 : Secret,
					ApiKeyName = ApiKeyName,
					ECPrivateKeyPem = ECPrivateKeyPem
				};
			}
			set
			{
				if (value == null) return;
				ApiKey = value.ApiKey;
				ApiSecretBase64 = !string.IsNullOrEmpty(value.ApiSecretBase64) ? value.ApiSecretBase64 : value.Secret;
				Secret = !string.IsNullOrEmpty(value.Secret) ? value.Secret : ApiSecretBase64;
				Passphrase = value.Passphrase;
				ApiKeyName = value.ApiKeyName;
				ECPrivateKeyPem = value.ECPrivateKeyPem;
			}
		}
	}

    [Serializable]
    public class ProfileData
    {
        public List<AccountInfo> Accounts = new List<AccountInfo>();
        public List<KeyInfo> Keys = new List<KeyInfo>();
        public List<int> BlockedHours = new List<int>();
        public decimal DefaultRiskPct = 0.5m;
        public string QuoteFilter = "USD";
        public int DefaultGranMinutes = 15;
    }
}
