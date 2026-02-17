
using System;
using System.Security.Cryptography;
using System.Text;

namespace CryptoDayTraderSuite.Util
{
    public static class SecurityUtil
    {
        public static string ComputeHmacSha256Base64(string secretBase64, string message)
        {
            /* coinbase exchange sign helper */
            var key = Convert.FromBase64String(secretBase64); /* decode */
            using (var h = new HMACSHA256(key))
            {
                var sig = h.ComputeHash(Encoding.UTF8.GetBytes(message)); /* compute */
                return Convert.ToBase64String(sig); /* base64 */
            }
        }

        public static string ComputeHmacSha256Hex(string secret, string message)
        {
            /* bitstamp hex uppercase */
            using (var h = new HMACSHA256(Encoding.ASCII.GetBytes(secret)))
            {
                var sig = h.ComputeHash(Encoding.ASCII.GetBytes(message)); /* compute */
                var sb = new StringBuilder(); /* build */
                for (int i = 0; i < sig.Length; i++) sb.Append(sig[i].ToString("x2")); /* hex */
                return sb.ToString().ToUpperInvariant(); /* upper */
            }
        }

        public static string ComputeHmacSha512Base64(string secretBase64, byte[] message)
        {
            /* kraken hmac sha512 with base64 secret */
            var key = Convert.FromBase64String(secretBase64); /* decode */
            using (var h = new HMACSHA512(key))
            {
                var sig = h.ComputeHash(message); /* compute */
                return Convert.ToBase64String(sig); /* base64 */
            }
        }

        public static byte[] Sha256(byte[] data)
        {
            using (var s = SHA256.Create())
            {
                return s.ComputeHash(data); /* sha256 */
            }
        }
    }
}
