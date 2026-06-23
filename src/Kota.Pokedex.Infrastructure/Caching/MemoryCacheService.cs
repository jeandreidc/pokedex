using System.Text.Json;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kota.Pokedex.Infrastructure.Caching;

public class MemoryCacheService : ICacheService {
    private static readonly JsonSerializerOptions JsonOptions = new();
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;

    public MemoryCacheService(IMemoryCache memoryCache, IOptions<CacheOptions> options) {
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) {
        if (_memoryCache.TryGetValue(key, out byte[]? bytes) && bytes is not null) {
            return Task.FromResult(JsonSerializer.Deserialize<T>(bytes, JsonOptions));
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        var ttl = expiry ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        _memoryCache.Set(key, bytes, ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}
