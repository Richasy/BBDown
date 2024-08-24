using Richasy.BiliKernel.Bili.Authorization;
using System.Collections.Generic;
using System.Linq;

namespace BBDown
{
    public sealed class CookieResolver : IBiliCookiesResolver
    {
        private static string _cookies = string.Empty;

        public CookieResolver(string cookies)
        {
            _cookies = cookies;
        }

        public IDictionary<string, string> GetCookies()
        {
            return _cookies.Split("; ").Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
        }

        public string GetCookieString()
            => _cookies;

        public void RemoveCookies()
            => _cookies = string.Empty;

        public void SaveCookies(IDictionary<string, string> cookies)
            => _cookies = string.Join("; ", cookies.Select(x => $"{x.Key}={x.Value}"));
    }
}
