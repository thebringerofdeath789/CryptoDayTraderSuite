/* File: Util/HttpUtil.cs */
/* fixes: no Content-Type on DefaultRequestHeaders to avoid 'Misused header name' */
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CryptoDayTraderSuite.Util
{
    public static class HttpUtil
    {
        private static readonly HttpClient _http = new HttpClient(); /* shared */
        private const string DefaultUserAgent = "CryptoDayTraderSuite/1.0";

        static HttpUtil()
        {
            try
            {
                if (!_http.DefaultRequestHeaders.UserAgent.TryParseAdd(DefaultUserAgent))
                {
                    _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
                }
            }
            catch { }
        }

        public static void ClearDefaultHeaders()
        {
            _http.DefaultRequestHeaders.Clear();
            try
            {
                if (!_http.DefaultRequestHeaders.UserAgent.TryParseAdd(DefaultUserAgent))
                {
                    _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
                }
            }
            catch { }
        }

        public static void AddDefaultHeader(string name, string value)
        {
            _http.DefaultRequestHeaders.Remove(name);
            _http.DefaultRequestHeaders.Add(name, value);
        }

        public static async Task<string> GetAsync(string url)
        {
            Log.Trace($"GET {url}");
            try 
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
                    using (var r = await _http.SendAsync(req))
                    {
                        var body = await r.Content.ReadAsStringAsync();
                        if (!r.IsSuccessStatusCode) 
                        {
                            var msg = $"HTTP {(int)r.StatusCode}: {body}";
                            LogHttpFailure(url, "GET", msg, r.IsSuccessStatusCode, (int)r.StatusCode);
                            throw new HttpRequestException(msg);
                        }
                        Log.Trace($"GET {url} OK ({body.Length} bytes)");
                        return body;
                    }
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (IsTransient(ex))
                {
                    Log.Warn($"GET {url} transient exception: {ex.Message}");
                }
                else
                {
                    Log.Error($"GET {url} EXCEPTION", ex);
                }
                throw;
            }
        }

        public static async Task<string> SendAsync(HttpRequestMessage req)
        {
            var method = req.Method != null ? req.Method.Method : "UNKNOWN";
            var url = req.RequestUri != null ? req.RequestUri.ToString() : "(null)";
            Log.Trace($"SEND {method} {url}");
            req.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent);

            try
            {
                var r = await _http.SendAsync(req);
                var body = await r.Content.ReadAsStringAsync();
                if (!r.IsSuccessStatusCode)
                {
                    var msg = $"HTTP {(int)r.StatusCode} {method} {url}: {body}";
                    LogHttpFailure(url, method, msg, r.IsSuccessStatusCode, (int)r.StatusCode);
                    throw new HttpRequestException(msg);
                }
                Log.Trace($"SEND {method} {url} OK ({body.Length} bytes)");
                return body;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var exMsg = ex != null ? ex.Message : string.Empty;
                if (!string.IsNullOrEmpty(exMsg) && exMsg.IndexOf("ua-header", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Log.Error($"[Connection] ua-header mismatch exception on {method} {url}", ex);
                }
                else if (IsTransient(ex))
                {
                    Log.Warn($"[Connection] Transient exception on {method} {url}: {ex.Message}");
                }
                else
                {
                    Log.Error($"[Connection] Exception on {method} {url}", ex);
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
                    if (attempt >= maxAttempts || !IsTransient(ex)) throw;
                    
                    /* Exponential Backoff */
                    int delay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    if (IsRateLimited(ex))
                    {
                        delay = Math.Max(delay, 2000 * attempt);
                        Log.Warn("[HTTP] Rate-limited (429). Backing off for " + delay + "ms before retry.", "RetryAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Util\\HttpUtil.cs", 129);
                    }
                    await Task.Delay(delay);
                }
            }
        }

        private static bool IsTransient(Exception ex)
        {
            /* Check for timeouts, 5xx errors, or network issues */
            /* Simplified check for now */
            var msg = ex.Message.ToLowerInvariant();
            if (msg.Contains("429") || msg.Contains("too many requests")) return true;
            if (msg.Contains("timeout")) return true;
            if (msg.Contains("500") || msg.Contains("502") || msg.Contains("503") || msg.Contains("504")) return true;
            /* request exception / socket exception usually implies network */
            if (ex is HttpRequestException) return true;
            return false;
        }

        private static bool IsRateLimited(Exception ex)
        {
            if (ex == null || string.IsNullOrWhiteSpace(ex.Message)) return false;
            var msg = ex.Message.ToLowerInvariant();
            return msg.Contains("429") || msg.Contains("too many requests");
        }

        private static void LogHttpFailure(string url, string method, string message, bool isSuccessStatusCode, int statusCode)
        {
            if (!string.IsNullOrEmpty(message) && message.IndexOf("ua-header", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Log.Error("[Connection] ua-header mismatch detected: " + message);
                return;
            }

            var isClientError = statusCode >= 400 && statusCode < 500;
            var urlLower = (url ?? string.Empty).ToLowerInvariant();
            var bybitRequest = urlLower.Contains("bybit");

            if (isClientError || bybitRequest)
            {
                Log.Warn("[Connection] Request failed: " + message);
                return;
            }

            Log.Error("[Connection] Request failed: " + message);
        }
    }
}