using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;

namespace UserAPI.Services.Impl
{
    public class TokenBlackListService : ITokenBlackListService
    {
        private readonly IDistributedCache _cache;

        public TokenBlackListService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task DeactivateTokenAsync(string token)
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var tokenId = jwtToken.Id;
            var expiry = jwtToken.ValidTo;
            var ttl = expiry - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero) return;
            await _cache.SetStringAsync(
                $"blacklist:{tokenId}",
                "revoked",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                }
                );
        }

        public async Task<bool> IsTokenBlackListedAsync(string token)
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var result = await _cache.GetStringAsync($"blacklist:{jwtToken.Id}");
            return result != null;
        }
    }
}
