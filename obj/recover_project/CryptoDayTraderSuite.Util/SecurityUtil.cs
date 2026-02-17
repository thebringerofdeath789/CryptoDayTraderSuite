using System;
using System.Security.Cryptography;
using System.Text;

namespace CryptoDayTraderSuite.Util
{
	public static class SecurityUtil
	{
		public static string ComputeHmacSha256Base64(string secretBase64, string message)
		{
			byte[] key = Convert.FromBase64String(secretBase64);
			using (HMACSHA256 h = new HMACSHA256(key))
			{
				byte[] sig = h.ComputeHash(Encoding.UTF8.GetBytes(message));
				return Convert.ToBase64String(sig);
			}
		}

		public static string ComputeHmacSha256Hex(string secret, string message)
		{
			using (HMACSHA256 h = new HMACSHA256(Encoding.ASCII.GetBytes(secret)))
			{
				byte[] sig = h.ComputeHash(Encoding.ASCII.GetBytes(message));
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < sig.Length; i++)
				{
					sb.Append(sig[i].ToString("x2"));
				}
				return sb.ToString().ToUpperInvariant();
			}
		}

		public static string ComputeHmacSha512Base64(string secretBase64, byte[] message)
		{
			byte[] key = Convert.FromBase64String(secretBase64);
			using (HMACSHA512 h = new HMACSHA512(key))
			{
				byte[] sig = h.ComputeHash(message);
				return Convert.ToBase64String(sig);
			}
		}

		public static byte[] Sha256(byte[] data)
		{
			using (SHA256 s = SHA256.Create())
			{
				return s.ComputeHash(data);
			}
		}
	}
}
