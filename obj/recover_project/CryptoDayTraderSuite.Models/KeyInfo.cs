using System;

namespace CryptoDayTraderSuite.Models
{
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

		public bool Active;

		public string Service
		{
			get
			{
				return Broker;
			}
			set
			{
				Broker = value;
			}
		}

		public KeyData Data
		{
			get
			{
				return new KeyData
				{
					ApiKey = ApiKey,
					Secret = Secret,
					Passphrase = Passphrase,
					ApiSecretBase64 = ((!string.IsNullOrEmpty(ApiSecretBase64)) ? ApiSecretBase64 : Secret),
					ApiKeyName = ApiKeyName,
					ECPrivateKeyPem = ECPrivateKeyPem
				};
			}
			set
			{
				if (value != null)
				{
					ApiKey = value.ApiKey;
					ApiSecretBase64 = ((!string.IsNullOrEmpty(value.ApiSecretBase64)) ? value.ApiSecretBase64 : value.Secret);
					Secret = ((!string.IsNullOrEmpty(value.Secret)) ? value.Secret : ApiSecretBase64);
					Passphrase = value.Passphrase;
					ApiKeyName = value.ApiKeyName;
					ECPrivateKeyPem = value.ECPrivateKeyPem;
				}
			}
		}
	}
}
