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
        // P1.2: same instance — no JSON clone
        ReferenceEquals(result, entries).Should().BeTrue();
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

    [Fact]
    public async Task GetOrCreateAsync_InvokesFactoryOnce_UnderConcurrency() {
        var calls = 0;
        var tasks = Enumerable.Range(0, 20).Select(_ =>
            _sut.GetOrCreateAsync(
                "stampede-key",
                async ct => {
                    Interlocked.Increment(ref calls);
                    await Task.Delay(50, ct);
                    return "value";
                }));

        var results = await Task.WhenAll(tasks);

        calls.Should().Be(1);
        results.Should().OnlyContain(r => r == "value");
    }
}
