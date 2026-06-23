using Microsoft.Extensions.Caching.Distributed;

namespace Kota.Pokedex.Tests.Unit.Helpers.Caching;

/// <summary>
/// In-memory IDistributedCache stand-in for Redis unit tests.
/// Tracks operations so tests can assert cache hit/miss behavior.
/// </summary>
public sealed class FakeDistributedCache : IDistributedCache {
    private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    public int RemoveCount { get; private set; }
    public IReadOnlyDictionary<string, byte[]> Store => _store;

    public byte[]? Get(string key) {
        GetCount++;
        return _store.TryGetValue(key, out var value) ? value : null;
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        Task.FromResult(Get(key));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) {
        SetCount++;
        _store[key] = value;
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key) { }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) {
        RemoveCount++;
        _store.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default) {
        Remove(key);
        return Task.CompletedTask;
    }

    public void ResetCounters() {
        GetCount = 0;
        SetCount = 0;
        RemoveCount = 0;
    }
}
