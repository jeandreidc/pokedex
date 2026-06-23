using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Infrastructure.Caching;
using Kota.Pokedex.Tests.Unit.Helpers.Caching;
using Kota.Pokedex.Tests.Unit.Helpers.Http;

namespace Kota.Pokedex.Tests.Unit.Infrastructure.Caching;

public class RedisCacheServiceTests {
    private readonly FakeDistributedCache _cache = new();
    private readonly RedisCacheService _sut;

    public RedisCacheServiceTests() {
        _sut = new RedisCacheService(_cache, TestOptions.Cache(ttlMinutes: 30));
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenKeyMissing() {
        var result = await _sut.GetAsync<string>("missing-key");

        result.Should().BeNull();
        _cache.GetCount.Should().Be(1);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsSameValue() {
        var payload = new List<int> { 1, 2, 3 };

        await _sut.SetAsync("test-key", payload);
        var result = await _sut.GetAsync<List<int>>("test-key");

        result.Should().BeEquivalentTo(payload);
        _cache.SetCount.Should().Be(1);
        _cache.GetCount.Should().Be(1);
    }

    [Fact]
    public async Task SetAsync_UsesDefaultTtl_WhenExpiryNotProvided() {
        await _sut.SetAsync("ttl-key", "value");

        _cache.Store.Should().ContainKey("ttl-key");
    }

    [Fact]
    public async Task SetAsync_UsesCustomExpiry_WhenProvided() {
        await _sut.SetAsync("custom-ttl", "value", TimeSpan.FromMinutes(5));

        _cache.Store.Should().ContainKey("custom-ttl");
    }

    [Fact]
    public async Task RemoveAsync_RemovesKeyFromCache() {
        await _sut.SetAsync("remove-me", 42);
        await _sut.RemoveAsync("remove-me");
        var result = await _sut.GetAsync<int>("remove-me");

        result.Should().Be(0);
        _cache.RemoveCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_DeserializesComplexTypes() {
        var entries = new[] {
            new { Id = 25, Name = "pikachu" },
            new { Id = 1, Name = "bulbasaur" }
        };

        await _sut.SetAsync(CacheKeys.PokemonIndex, entries);
        var result = await _sut.GetAsync<object[]>(CacheKeys.PokemonIndex);

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
    }

    [Fact]
    public async Task SecondGet_DoesNotCallSetAgain() {
        await _sut.SetAsync("hit-test", "cached");
        _cache.ResetCounters();

        await _sut.GetAsync<string>("hit-test");
        await _sut.GetAsync<string>("hit-test");

        _cache.GetCount.Should().Be(2);
        _cache.SetCount.Should().Be(0);
    }
}
