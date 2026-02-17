using System;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class AccountProfile
	{
		public string Id { get; set; }

		public string Label { get; set; }

		public string Service { get; set; }

		public string Mode { get; set; }

		public string DefaultQuote { get; set; }

		public bool Paper { get; set; }

		public decimal MaxOrderPct { get; set; }

		public decimal RiskPerTradePct { get; set; }

		public int MaxConcurrentTrades { get; set; }

		public string KeyEntryId { get; set; }

		public DateTime CreatedUtc { get; set; }

		public bool Enabled { get; set; }

		public DateTime UpdatedUtc { get; set; }

		public static implicit operator AccountProfile(AccountInfo a)
		{
			if (a == null)
			{
				return null;
			}
			return new AccountProfile
			{
				Id = a.Id,
				Label = a.Label,
				Service = a.Service,
				Mode = a.Mode.ToString(),
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
			if (p == null)
			{
				return null;
			}
			AccountInfo info = new AccountInfo
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
				CreatedUtc = ((p.CreatedUtc == default(DateTime)) ? DateTime.UtcNow : p.CreatedUtc),
				Enabled = p.Enabled,
				UpdatedUtc = p.UpdatedUtc
			};
			info.Mode = (Enum.TryParse<AccountMode>(p.Mode ?? "Live", ignoreCase: true, out var m) ? m : AccountMode.Live);
			return info;
		}
	}
}
