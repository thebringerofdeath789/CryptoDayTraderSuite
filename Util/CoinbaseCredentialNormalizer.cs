using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Util
{
    public static class CoinbaseCredentialNormalizer
    {
        public static void NormalizeCoinbaseAdvancedInputs(ref string apiKey, ref string apiSecret, ref string apiKeyName, ref string ecPrivateKeyPem)
        {
            var key = apiKey ?? string.Empty;
            var secret = apiSecret ?? string.Empty;
            var keyName = apiKeyName ?? string.Empty;
            var privateKeyPem = ecPrivateKeyPem ?? string.Empty;

            string parsedKeyName;
            string parsedPrivateKey;

            if (TryExtractNameAndPrivateKey(key, out parsedKeyName, out parsedPrivateKey))
            {
                if (string.IsNullOrWhiteSpace(keyName)) keyName = parsedKeyName;
                if (string.IsNullOrWhiteSpace(privateKeyPem)) privateKeyPem = parsedPrivateKey;
            }

            if (TryExtractNameAndPrivateKey(secret, out parsedKeyName, out parsedPrivateKey))
            {
                if (string.IsNullOrWhiteSpace(keyName)) keyName = parsedKeyName;
                if (string.IsNullOrWhiteSpace(privateKeyPem)) privateKeyPem = parsedPrivateKey;
            }

            if (TryExtractNameAndPrivateKey(privateKeyPem, out parsedKeyName, out parsedPrivateKey))
            {
                if (string.IsNullOrWhiteSpace(keyName)) keyName = parsedKeyName;
                privateKeyPem = parsedPrivateKey;
            }

            if (string.IsNullOrWhiteSpace(keyName) && LooksLikeApiKeyName(key))
            {
                keyName = key.Trim();
            }

            if (string.IsNullOrWhiteSpace(privateKeyPem) && LooksLikePem(secret))
            {
                privateKeyPem = secret;
            }

            if (!string.IsNullOrWhiteSpace(privateKeyPem))
            {
                privateKeyPem = NormalizePem(privateKeyPem);
            }

            apiKey = key;
            apiSecret = secret;
            apiKeyName = keyName;
            ecPrivateKeyPem = privateKeyPem;
        }

        public static bool TryExtractNameAndPrivateKey(string raw, out string apiKeyName, out string privateKeyPem)
        {
            apiKeyName = string.Empty;
            privateKeyPem = string.Empty;

            if (string.IsNullOrWhiteSpace(raw)) return false;
            var trimmed = raw.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) return false;

            Dictionary<string, object> payload;
            try
            {
                payload = UtilCompat.JsonDeserialize<Dictionary<string, object>>(trimmed);
            }
            catch
            {
                return false;
            }

            if (payload == null) return false;

            var name = Read(payload, "name");
            var privateKey = Read(payload, "privateKey");

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(privateKey))
            {
                return false;
            }

            apiKeyName = name.Trim();
            privateKeyPem = NormalizePem(privateKey);
            return true;
        }

        public static string NormalizePem(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var normalized = value.Trim();
            normalized = normalized.Replace("\\r\\n", "\n");
            normalized = normalized.Replace("\\n", "\n");
            normalized = normalized.Replace("\r\n", "\n");
            normalized = normalized.Replace("\r", "\n");
            return normalized;
        }

        public static bool LooksLikeApiKeyName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var trimmed = value.Trim();
            return trimmed.IndexOf("organizations/", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   trimmed.IndexOf("/apiKeys/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool LooksLikePem(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.IndexOf("BEGIN EC PRIVATE KEY", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("BEGIN PRIVATE KEY", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Read(Dictionary<string, object> payload, string key)
        {
            if (payload == null || string.IsNullOrWhiteSpace(key)) return string.Empty;

            object value;
            if (!payload.TryGetValue(key, out value) || value == null) return string.Empty;
            return value.ToString();
        }
    }
}