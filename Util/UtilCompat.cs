using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace CryptoDayTraderSuite.Util
{
    public static class UtilCompat
    {
        public static string JsonSerialize<T>(T obj)
        {
            var js = new JavaScriptSerializer();
            return js.Serialize(obj);
        }
        public static T JsonDeserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);
            var js = new JavaScriptSerializer();
            return js.Deserialize<T>(json);
        }
        public static string HttpGet(string url, string accept = "application/json")
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            if (!string.IsNullOrEmpty(accept)) req.Accept = accept;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }
        public static string HttpPost(string url, string contentType, string body, Dictionary<string,string> headers = null)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            if (!string.IsNullOrEmpty(contentType)) req.ContentType = contentType;
            if (headers != null) foreach (var kv in headers) req.Headers[kv.Key] = kv.Value ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(body ?? "");
            req.ContentLength = bytes.Length;
            using (var rs = req.GetRequestStream()) { rs.Write(bytes, 0, bytes.Length); }
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }
    }
}
