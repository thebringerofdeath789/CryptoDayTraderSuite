using System;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class KeyEntry
	{
		public string Id { get; set; }

		public string Broker { get; set; }

		public string Label { get; set; }

		public string ApiKey { get; set; }

		public string Secret { get; set; }

		public string Passphrase { get; set; }

		public string Service { get; set; }

		public DateTime UpdatedUtc { get; set; }

		public DateTime CreatedUtc { get; set; }

		public bool Enabled { get; set; }

		public bool Active { get; set; }

		public KeyData Data { get; set; } = new KeyData();

		public static string MakeId(string broker, string label)
		{
			if (broker == null)
			{
				broker = "";
			}
			if (label == null)
			{
				label = "";
			}
			return broker + "|" + label;
		}

		public static void SplitId(string id, out string broker, out string label)
		{
			broker = "";
			label = "";
			if (!string.IsNullOrEmpty(id))
			{
				int i = id.IndexOf('|');
				if (i >= 0)
				{
					broker = id.Substring(0, i);
					label = ((i + 1 < id.Length) ? id.Substring(i + 1) : "");
				}
				else
				{
					label = id;
				}
			}
		}

		public static implicit operator KeyEntry(KeyInfo k)
		{
			if (k == null)
			{
				return null;
			}
			return new KeyEntry
			{
				Id = MakeId(k.Broker, k.Label),
				Broker = k.Broker,
				Label = k.Label,
				ApiKey = k.ApiKey,
				Secret = ((!string.IsNullOrEmpty(k.ApiSecretBase64)) ? k.ApiSecretBase64 : k.Secret),
				Passphrase = k.Passphrase,
				CreatedUtc = k.CreatedUtc,
				Enabled = k.Enabled,
				Data = (k.Data ?? new KeyData())
			};
		}

		public static explicit operator KeyInfo(KeyEntry k)
		{
			if (k == null)
			{
				return null;
			}
			return new KeyInfo
			{
				Broker = k.Broker,
				Label = k.Label,
				ApiKey = k.ApiKey,
				Secret = k.Secret,
				ApiSecretBase64 = ((k.Data != null) ? k.Data.ApiSecretBase64 : k.Secret),
				ApiKeyName = ((k.Data != null) ? k.Data.ApiKeyName : null),
				ECPrivateKeyPem = ((k.Data != null) ? k.Data.ECPrivateKeyPem : null),
				Passphrase = k.Passphrase,
				CreatedUtc = k.CreatedUtc,
				Enabled = k.Enabled,
				Active = k.Active,
				Service = k.Service,
				Data = k.Data
			};
		}
	}
}
