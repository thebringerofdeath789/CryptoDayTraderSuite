using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CryptoDayTraderSuite.Util
{
	public static class HttpUtil
	{
		private static readonly HttpClient _http;

		private const string DefaultUserAgent = "CryptoDayTraderSuite/1.0";

		static HttpUtil()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Expected O, but got Unknown
			_http = new HttpClient();
			try
			{
				if (!_http.DefaultRequestHeaders.UserAgent.TryParseAdd("CryptoDayTraderSuite/1.0"))
				{
					((HttpHeaders)_http.DefaultRequestHeaders).TryAddWithoutValidation("User-Agent", "CryptoDayTraderSuite/1.0");
				}
			}
			catch
			{
			}
		}

		public static void ClearDefaultHeaders()
		{
			((HttpHeaders)_http.DefaultRequestHeaders).Clear();
			try
			{
				if (!_http.DefaultRequestHeaders.UserAgent.TryParseAdd("CryptoDayTraderSuite/1.0"))
				{
					((HttpHeaders)_http.DefaultRequestHeaders).TryAddWithoutValidation("User-Agent", "CryptoDayTraderSuite/1.0");
				}
			}
			catch
			{
			}
		}

		public static void AddDefaultHeader(string name, string value)
		{
			((HttpHeaders)_http.DefaultRequestHeaders).Remove(name);
			((HttpHeaders)_http.DefaultRequestHeaders).Add(name, value);
		}

		public static async Task<string> GetAsync(string url)
		{
			Log.Trace("GET " + url, "GetAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 47);
			try
			{
				HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
				try
				{
					((HttpHeaders)req.Headers).TryAddWithoutValidation("User-Agent", "CryptoDayTraderSuite/1.0");
					HttpResponseMessage r = await _http.SendAsync(req);
					try
					{
						string body = await r.Content.ReadAsStringAsync();
						if (!r.IsSuccessStatusCode)
						{
							string msg = $"HTTP {(int)r.StatusCode}: {body}";
							Log.Debug("GET " + url + " FAILED: " + msg, "GetAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 59);
							throw new Exception(msg);
						}
						Log.Trace($"GET {url} OK ({body.Length} bytes)", "GetAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 62);
						return body;
					}
					finally
					{
						((IDisposable)r)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)req)?.Dispose();
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("GET " + url + " EXCEPTION", ex2, "GetAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 69);
				throw;
			}
		}

		public static async Task<string> SendAsync(HttpRequestMessage req)
		{
			string method = ((req.Method != (HttpMethod)null) ? req.Method.Method : "UNKNOWN");
			string url = ((req.RequestUri != null) ? req.RequestUri.ToString() : "(null)");
			Log.Trace("SEND " + method + " " + url, "SendAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 78);
			((HttpHeaders)req.Headers).TryAddWithoutValidation("User-Agent", "CryptoDayTraderSuite/1.0");
			try
			{
				HttpResponseMessage r = await _http.SendAsync(req);
				string body = await r.Content.ReadAsStringAsync();
				if (!r.IsSuccessStatusCode)
				{
					string msg = $"HTTP {(int)r.StatusCode} {method} {url}: {body}";
					if (!string.IsNullOrEmpty(body) && body.IndexOf("ua-header", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						Log.Error("[Connection] ua-header mismatch detected: " + msg, null, "SendAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 90);
					}
					else
					{
						Log.Error("[Connection] Request failed: " + msg, null, "SendAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 94);
					}
					throw new Exception(msg);
				}
				Log.Trace($"SEND {method} {url} OK ({body.Length} bytes)", "SendAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 98);
				return body;
			}
			catch (Exception ex)
			{
				string exMsg = ((ex != null) ? ex.Message : string.Empty);
				if (!string.IsNullOrEmpty(exMsg) && exMsg.IndexOf("ua-header", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Log.Error("[Connection] ua-header mismatch exception on " + method + " " + url, ex, "SendAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 106);
				}
				else
				{
					Log.Error("[Connection] Exception on " + method + " " + url, ex, "SendAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 110);
				}
				throw;
			}
		}

		public static async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxAttempts = 3, int initialDelayMs = 500)
		{
			int attempt = 0;
			while (true)
			{
				attempt++;
				try
				{
					return await operation();
				}
				catch (Exception ex)
				{
					if (attempt >= maxAttempts || !IsTransient(ex))
					{
						throw;
					}
					int delay = initialDelayMs * (int)Math.Pow(2.0, attempt - 1);
					await Task.Delay(delay);
				}
			}
		}

		private static bool IsTransient(Exception ex)
		{
			string msg = ex.Message.ToLowerInvariant();
			if (msg.Contains("timeout"))
			{
				return true;
			}
			if (msg.Contains("500") || msg.Contains("502") || msg.Contains("503") || msg.Contains("504"))
			{
				return true;
			}
			if (ex is HttpRequestException)
			{
				return true;
			}
			return false;
		}
	}
}
