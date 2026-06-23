using System.Text.Json;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Kota.Pokedex.Infrastructure.Caching;

public class RedisCacheService : ICacheService {
    private static readonly JsonSerializerOptions JsonOptions = new();
    private readonly IDistributedCache _distributedCache;
    private readonly CacheOptions _options;

    public RedisCacheService(IDistributedCache distributedCache, IOptions<CacheOptions> options) {
        _distributedCache = distributedCache;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) {
        var bytes = await _distributedCache.GetAsync(key, cancellationToken);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        var ttl = expiry ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        await _distributedCache.SetAsync(key, bytes, new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = ttl
        }, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _distributedCache.RemoveAsync(key, cancellationToken);
}
