using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models.PokeApi;
using Kota.Pokedex.Infrastructure.Caching;
using Kota.Pokedex.Infrastructure.Services;
using Kota.Pokedex.Tests.Unit.Fixtures.PokeApi;
using Kota.Pokedex.Tests.Unit.Helpers.Caching;
using Kota.Pokedex.Tests.Unit.Helpers.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kota.Pokedex.Tests.Unit.Infrastructure.Services;

public class FilterMetadataServiceTests {
    private readonly Mock<IPokeApiClient> _pokeApi = new();
    private readonly FakeDistributedCache _distributedCache = new();
    private readonly FilterMetadataService _sut;

    public FilterMetadataServiceTests() {
        var cache = new RedisCacheService(_distributedCache, TestOptions.Cache());
        _sut = new FilterMetadataService(
            _pokeApi.Object,
            cache,
            TestOptions.Cache(),
            NullLogger<FilterMetadataService>.Instance);

        _pokeApi.Setup(c => c.GetTypeListAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.TypeList());
        _pokeApi.Setup(c => c.GetAbilityListAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.AbilityList());
        _pokeApi.Setup(c => c.GetGenerationListAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokeApiFixtures.GenerationList());
    }

    [Fact]
    public async Task GetTypesAsync_LoadsAndCachesTypes() {
        var result = await _sut.GetTypesAsync();

        result.Should().HaveCount(4);
        result.Should().Contain(t => t.Name == "fire" && t.DisplayName == "Fire");
        _distributedCache.Store.Should().ContainKey(CacheKeys.FilterTypes);
    }

    [Fact]
    public async Task GetTypesAsync_UsesCacheOnSecondCall() {
        await _sut.GetTypesAsync();
        _pokeApi.Invocations.Clear();

        await _sut.GetTypesAsync();

        _pokeApi.Verify(c => c.GetTypeListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAbilitiesAsync_LoadsAndCachesAbilities() {
        var result = await _sut.GetAbilitiesAsync();

        result.Should().HaveCount(15);
        result.First(a => a.Name == "overgrow").DisplayName.Should().Be("Overgrow");
        _distributedCache.Store.Should().ContainKey(CacheKeys.FilterAbilities);
    }

    [Fact]
    public async Task GetAbilitiesAsync_LoadsMultiplePages() {
        _pokeApi.Setup(c => c.GetAbilityListAsync(100, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PokeApiListResponse {
                Count = 120,
                Next = $"{PokeApiFixtures.BaseUrl}ability?offset=100",
                Results = Enumerable.Range(1, 100)
                    .Select(i => PokeApiFixtures.NamedResource($"ability-{i}", $"ability/{i}/"))
                    .ToList()
            });
        _pokeApi.Setup(c => c.GetAbilityListAsync(100, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PokeApiListResponse {
                Count = 120,
                Results = Enumerable.Range(101, 20)
                    .Select(i => PokeApiFixtures.NamedResource($"ability-{i}", $"ability/{i}/"))
                    .ToList()
            });

        var result = await _sut.GetAbilitiesAsync();

        result.Should().HaveCount(120);
        _pokeApi.Verify(c => c.GetAbilityListAsync(100, 0, It.IsAny<CancellationToken>()), Times.Once);
        _pokeApi.Verify(c => c.GetAbilityListAsync(100, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGenerationsAsync_FormatsRomanNumerals() {
        var result = await _sut.GetGenerationsAsync();

        result.Should().HaveCount(3);
        result.Should().Contain(g => g.Name == "generation-i" && g.DisplayName == "Generation I");
        result.Should().Contain(g => g.Name == "generation-ii" && g.DisplayName == "Generation II");
        _distributedCache.Store.Should().ContainKey(CacheKeys.FilterGenerations);
    }

    [Fact]
    public async Task WarmupAsync_PrefetchesAllFilterLists() {
        await _sut.WarmupAsync();

        _distributedCache.Store.Keys.Should().Contain(CacheKeys.FilterTypes);
        _distributedCache.Store.Keys.Should().Contain(CacheKeys.FilterAbilities);
        _distributedCache.Store.Keys.Should().Contain(CacheKeys.FilterGenerations);
    }
}
