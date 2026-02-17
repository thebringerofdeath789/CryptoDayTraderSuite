using System;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class KeyData
	{
		public string ApiKey;

		public string Secret;

		public string Passphrase;

		public string ApiKeyName;

		public string ECPrivateKeyPem;

		public string ApiSecretBase64;

		public string this[string key]
		{
			get
			{
				TryGetValue(key, out var v);
				return v;
			}
			set
			{
				switch (Canon(key))
				{
				case "apikey":
					ApiKey = value;
					break;
				case "secret":
					Secret = value;
					break;
				case "passphrase":
					Passphrase = value;
					break;
				case "apikeyname":
					ApiKeyName = value;
					break;
				case "ecprivatekeypem":
					ECPrivateKeyPem = value;
					break;
				case "apisecretbase64":
					ApiSecretBase64 = value;
					break;
				}
			}
		}

		private static string Canon(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return "";
			}
			string k = key.Trim().ToLowerInvariant();
			if (k == "apikey" || k == "api_key" || k == "key")
			{
				return "apikey";
			}
			switch (k)
			{
			default:
				if (!(k == "private_key"))
				{
					if (k == "passphrase" || k == "phrase" || k == "pass")
					{
						return "passphrase";
					}
					switch (k)
					{
					case "apikeyname":
						return "apikeyname";
					case "ecprivatekeypem":
						return "ecprivatekeypem";
					case "apisecretbase64":
						return "apisecretbase64";
					default:
						return k;
					}
				}
				goto case "secret";
			case "secret":
			case "apisecret":
			case "api_secret":
			case "privatekey":
				return "secret";
			}
		}

		public bool TryGetValue(string key, out string value)
		{
			value = null;
			switch (Canon(key))
			{
			case "apikey":
				value = ApiKey;
				break;
			case "secret":
				value = Secret;
				break;
			case "passphrase":
				value = Passphrase;
				break;
			case "apikeyname":
				value = ApiKeyName;
				break;
			case "ecprivatekeypem":
				value = ECPrivateKeyPem;
				break;
			case "apisecretbase64":
				value = ApiSecretBase64;
				break;
			default:
				return false;
			}
			return !string.IsNullOrEmpty(value);
		}

		public bool ContainsKey(string key)
		{
			string v;
			return TryGetValue(key, out v);
		}
	}
}
