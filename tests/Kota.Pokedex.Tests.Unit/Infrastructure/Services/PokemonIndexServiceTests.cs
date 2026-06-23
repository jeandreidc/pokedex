using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Infrastructure.Caching;
using Kota.Pokedex.Infrastructure.Services;
using Kota.Pokedex.Tests.Unit.Fixtures.PokeApi;
using Kota.Pokedex.Tests.Unit.Helpers.Caching;
using Kota.Pokedex.Tests.Unit.Helpers.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kota.Pokedex.Tests.Unit.Infrastructure.Services;

public class PokemonIndexServiceTests {
    private readonly Mock<IPokeApiClient> _pokeApi = new();
    private readonly FakeDistributedCache _distributedCache = new();
    private readonly ICacheService _cacheService;
    private readonly PokemonIndexService _sut;

    public PokemonIndexServiceTests() {
        _cacheService = new RedisCacheService(_distributedCache, TestOptions.Cache());
        _sut = new PokemonIndexService(
            _pokeApi.Object,
            _cacheService,
            TestOptions.PokeApi(pageFetchLimit: 10),
            TestOptions.Cache(),
            NullLogger<PokemonIndexService>.Instance);

        SetupPokeApiDefaults();
    }

    [Fact]
    public async Task GetIndexAsync_FetchesFromApi_AndCachesOnFirstCall() {
        var result = await _sut.GetIndexAsync();

        result.Should().HaveCount(25);
        result[0].Name.Should().Be("bulbasaur");
        _pokeApi.Verify(c => c.GetPokemonListAsync(10, 0, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _distributedCache.SetCount.Should().BeGreaterThan(0);
        _distributedCache.Store.Should().ContainKey(CacheKeys.PokemonIndex);
    }

    [Fact]
    public async Task GetIndexAsync_ReturnsCachedValue_OnSecondCall() {
        await _sut.GetIndexAsync();
        _pokeApi.Invocations.Clear();
        _distributedCache.ResetCounters();

        var result = await _sut.GetIndexAsync();

        result.Should().HaveCount(25);
        _pokeApi.Verify(c => c.GetPokemonListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _distributedCache.GetCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPokemonIdsByTypeAsync_CachesResult() {
        var first = await _sut.GetPokemonIdsByTypeAsync("fire");
        _pokeApi.Invocations.Clear();
        _distributedCache.ResetCounters();

        var second = await _sut.GetPokemonIdsByTypeAsync("fire");

        first.Should().BeEquivalentTo(new[] { 4, 5, 6 });
        second.Should().BeEquivalentTo(first);
        _pokeApi.Verify(c => c.GetTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _distributedCache.Store.Should().ContainKey(CacheKeys.Type("fire"));
    }

    [Fact]
    public async Task GetPokemonIdsByAbilityAsync_CachesResult() {
        await _sut.GetPokemonIdsByAbilityAsync("overgrow");
        _pokeApi.Invocations.Clear();

        var result = await _sut.GetPokemonIdsByAbilityAsync("overgrow");

        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        _pokeApi.Verify(c => c.GetAbilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPokemonIdsByGenerationAsync_MapsSpeciesToIndexIds() {
        var result = await _sut.GetPokemonIdsByGenerationAsync("1");

        result.Should().HaveCount(25);
        result.Should().Contain(25);
        _distributedCache.Store.Should().ContainKey(CacheKeys.Generation("1"));
    }

    [Fact]
    public async Task GetEntryAsync_ReturnsEntryById() {
        await _sut.GetIndexAsync();

        var entry = await _sut.GetEntryAsync(25);

        entry.Should().NotBeNull();
        entry!.Name.Should().Be("pikachu");
    }

    [Fact]
    public async Task GetTypesForPokemonAsync_CachesTypes() {
        var first = await _sut.GetTypesForPokemonAsync(25);
        _pokeApi.Invocations.Clear();

        var second = await _sut.GetTypesForPokemonAsync(25);

        first.Should().Contain("electric");
        second.Should().BeEquivalentTo(first);
        _pokeApi.Verify(c => c.GetPokemonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _distributedCache.Store.Should().ContainKey(CacheKeys.PokemonDetail(25));
    }

    [Fact]
    public async Task GetIndexAsync_FetchesMultiplePages_WhenApiPaginates() {
        _pokeApi.Setup(c => c.GetPokemonListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int limit, int offset, CancellationToken _) =>
                PokeApiFixtures.PokemonListPage(offset, limit, 25));

        var result = await _sut.GetIndexAsync();

        result.Should().HaveCount(25);
        _pokeApi.Verify(c => c.GetPokemonListAsync(10, 0, It.IsAny<CancellationToken>()), Times.Once);
        _pokeApi.Verify(c => c.GetPokemonListAsync(10, 10, It.IsAny<CancellationToken>()), Times.Once);
        _pokeApi.Verify(c => c.GetPokemonListAsync(10, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEntryAsync_ReturnsNull_WhenIdNotInIndex() {
        _pokeApi.Setup(c => c.GetPokemonListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.PokemonListPage(0, 10, 5));

        var entry = await _sut.GetEntryAsync(999);

        entry.Should().BeNull();
    }

    [Fact]
    public async Task WarmupAsync_LoadsIndex() {
        await _sut.WarmupAsync();

        _distributedCache.Store.Should().ContainKey(CacheKeys.PokemonIndex);
    }

    private void SetupPokeApiDefaults() {
        _pokeApi.Setup(c => c.GetPokemonListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int limit, int offset, CancellationToken _) =>
                PokeApiFixtures.PokemonListPage(offset, limit, 25));

        _pokeApi.Setup(c => c.GetTypeAsync("fire", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.FireTypeDetail());

        _pokeApi.Setup(c => c.GetAbilityAsync("overgrow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.OvergrowAbility());

        _pokeApi.Setup(c => c.GetGenerationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.GenerationOneDetail());

        _pokeApi.Setup(c => c.GetPokemonAsync("25", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.PokemonDetail(25, "electric"));
    }
}
