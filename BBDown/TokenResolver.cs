using Richasy.BiliKernel.Bili.Authorization;
using Richasy.BiliKernel.Models.Authorization;

namespace BBDown
{
    public sealed class TokenResolver : IBiliTokenResolver
    {
        private string _token = string.Empty;

        public TokenResolver(string token)
        {
            _token = token;
        }

        public BiliToken? GetToken()
        {
            return new BiliToken { AccessToken = _token };
        }

        public void RemoveToken()
        {
            _token = string.Empty;
        }

        public void SaveToken(BiliToken token)
        {
            _token = token.AccessToken ?? string.Empty;
        }
    }
}
