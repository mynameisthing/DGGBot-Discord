using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace DGGBot.Utilities
{
    public class OAuthHeader
    {
        private static readonly string _chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private readonly Dictionary<string, string> _params;

        public OAuthHeader(string consumerKey,
            string consumerSecret,
            string token,
            string tokenSecret)
        {
            _params = new Dictionary<string, string>();
            _params["timestamp"] = GenerateTimeStamp();
            _params["nonce"] = GenerateNonce(32);
            _params["signature_method"] = "HMAC-SHA1";
            _params["signature"] = "";
            _params["version"] = "1.0";
            _params["consumer_key"] = consumerKey;
            _params["consumer_secret"] = consumerSecret;
            _params["token"] = token;
            _params["token_secret"] = tokenSecret;
        }

        public AuthenticationHeaderValue Generate(string uri, string method)
        {
            _params["signature"] = GenerateSignature(uri, method);
            var authHeader = EncodeRequestParameters(_params);
            return new AuthenticationHeaderValue("OAuth", authHeader);
        }

        private static string EncodeRequestParameters(Dictionary<string, string> p)
        {
            var stringBuilder = new StringBuilder();
            foreach (var item in p.OrderBy(x => x.Key))
                if (!string.IsNullOrEmpty(item.Value) &&
                    !item.Key.EndsWith("secret"))
                    stringBuilder.AppendFormat("oauth_{0}=\"{1}\", ",
                        item.Key,
                        Uri.EscapeDataString(item.Value));

            return stringBuilder.ToString().TrimEnd(' ').TrimEnd(',');
        }

        private string GenerateSignature(string uri, string method)
        {
            var signatureBase = GetSignatureBase(uri, method);

            var keystring = string.Format("{0}&{1}",
                Uri.EscapeDataString(_params["consumer_secret"]),
                Uri.EscapeDataString(_params["token_secret"]));

            var hash = new HMACSHA1 {Key = Encoding.ASCII.GetBytes(keystring)};

            var dataBuffer = Encoding.ASCII.GetBytes(signatureBase);
            var hashBytes = hash.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }

        private string GetSignatureBase(string url, string method)
        {
            var uri = new Uri(url);
            var normUrl = string.Format("{0}://{1}{2}", uri.Scheme, uri.Host, uri.AbsolutePath);

            // the sigbase starts with the method and the encoded URI
            var sb = new StringBuilder();
            sb.Append(method)
                .Append('&')
                .Append(Uri.EscapeDataString(normUrl))
                .Append('&');


            // The parameters follow. This must include all oauth params
            // plus any query params on the uri.  Also, each uri may
            // have a distinct set of query params.

            // first, get the query params
            var queryParameters = ExtractQueryParameters(uri.Query);

            // add to that list all non-empty oauth params
            foreach (var pair in _params)
                // Exclude all oauth params that are secret or
                // signatures; any secrets must not be shared,
                // and any existing signature will be invalid.

                if (!string.IsNullOrEmpty(_params[pair.Key]) &&
                    !pair.Key.EndsWith("_secret") &&
                    !pair.Key.EndsWith("signature"))
                    queryParameters.Add("oauth_" + pair.Key, Uri.EscapeDataString(pair.Value));


            var stringBuilder = new StringBuilder();
            foreach (var item in queryParameters.OrderBy(x => x.Key))
                stringBuilder.AppendFormat("{0}={1}&", item.Key, item.Value);

            // append the Uri.EscapeDataStringd version of that string to the sigbase
            sb.Append(Uri.EscapeDataString(stringBuilder.ToString().TrimEnd('&')));
            var result = sb.ToString();


            return result;
        }

        private Dictionary<string, string> ExtractQueryParameters(string queryString)
        {
            if (queryString.StartsWith("?"))
                queryString = queryString.Remove(0, 1);

            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(queryString))
                return result;

            foreach (var query in queryString.Split('&'))
                if (!string.IsNullOrEmpty(query) && !query.StartsWith("oauth_"))
                    if (query.IndexOf('=') > -1)
                    {
                        var temp = query.Split('=');
                        result.Add(temp[0], temp[1]);
                    }
                    else
                    {
                        result.Add(query, string.Empty);
                    }

            return result;
        }

        private string GenerateNonce(int length)
        {
            var result = new StringBuilder();
            for (var i = 0; i < length; i++) result.Append(_chars[new Random().Next(0, 25)]);
            return result.ToString();
        }

        private string GenerateTimeStamp()
        {
            var ts = DateTime.UtcNow - _epoch;
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
    }
}