using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Infrastructure.Caching;
using Kota.Pokedex.Tests.Unit.Fixtures.Index;
using Kota.Pokedex.Tests.Unit.Helpers.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Kota.Pokedex.Tests.Unit.Infrastructure.Caching;

public class MemoryCacheServiceTests {
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly MemoryCacheService _sut;

    public MemoryCacheServiceTests() {
        _sut = new MemoryCacheService(_memoryCache, TestOptions.Cache(ttlMinutes: 30));
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenKeyMissing() {
        var result = await _sut.GetAsync<string>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsSameValue() {
        var entries = PokemonIndexFixtures.AllEntries.ToList();

        await _sut.SetAsync(CacheKeys.PokemonIndex, entries);
        var result = await _sut.GetAsync<List<Kota.Pokedex.Core.Models.PokemonIndexEntry>>(CacheKeys.PokemonIndex);

        result.Should().BeEquivalentTo(entries);
    }

    [Fact]
    public async Task RemoveAsync_RemovesCachedValue() {
        await _sut.SetAsync("key", "value");
        await _sut.RemoveAsync("key");

        var result = await _sut.GetAsync<string>("key");
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_StoresHashSetCorrectly() {
        var ids = PokemonIndexFixtures.FireTypeIds.ToHashSet();

        await _sut.SetAsync(CacheKeys.Type("fire"), ids);
        var result = await _sut.GetAsync<HashSet<int>>(CacheKeys.Type("fire"));

        result.Should().BeEquivalentTo(ids);
    }
}
