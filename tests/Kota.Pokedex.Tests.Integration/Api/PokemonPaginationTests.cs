using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Tests.Integration.Fixtures;
using Kota.Pokedex.Tests.Integration.Support;

namespace Kota.Pokedex.Tests.Integration.Api;

[Collection(nameof(PokedexIntegrationCollection))]
public sealed class PokemonPaginationTests(PokedexWebApplicationFactory factory) {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_FirstPage_ReturnsCorrectPaginationMetadata() {
        var response = await _client.GetAsync("/api/pokemon?page=1");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(PokemonPagination.CatalogPageSize);
        page.TotalCount.Should().Be(IntegrationPokeApiFixtures.TotalPokemon);
        page.TotalPages.Should().Be(2);
        page.Items.Should().HaveCount(PokemonPagination.CatalogPageSize);
        page.Items.Select(i => i.Id).Should().Equal(Enumerable.Range(1, PokemonPagination.CatalogPageSize));
        page.Items.Should().AllSatisfy(i => {
            i.Name.Should().NotBeNullOrWhiteSpace();
            i.SpriteUrl.Should().NotBeNullOrWhiteSpace();
            i.Types.Should().NotBeEmpty();
            i.Abilities.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task Get_SecondPage_ReturnsNextBatch() {
        var response = await _client.GetAsync("/api/pokemon?page=2");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(2);
        page.PageSize.Should().Be(PokemonPagination.CatalogPageSize);
        page.Items.Should().HaveCount(1);
        page.Items.Select(i => i.Id).Should().Equal(25);
    }

    [Fact]
    public async Task Get_PageZero_NormalizesToFirstPage() {
        var response = await _client.GetAsync("/api/pokemon?page=0");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.Items.Should().HaveCount(PokemonPagination.CatalogPageSize);
    }

    [Fact]
    public async Task Get_ClientPageSize_IsIgnoredAndCatalogPageSizeIsUsed() {
        var response = await _client.GetAsync("/api/pokemon?page=1&pageSize=500");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.PageSize.Should().Be(PokemonPagination.CatalogPageSize);
        page.Items.Should().HaveCount(PokemonPagination.CatalogPageSize);
    }

    [Fact]
    public async Task Get_WithSearchAndPagination_ReturnsFilteredPage() {
        var response = await _client.GetAsync("/api/pokemon?search=char&page=1");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(1);
        page.Items.Should().HaveCount(3);
        page.Items.Select(i => i.Name).Should().Equal("charmander", "charmeleon", "charizard");
    }

    private sealed class PagedResultJson<T> {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class PokemonSummaryJson {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string SpriteUrl { get; set; } = "";
        public List<string> Types { get; set; } = [];
        public List<string> Abilities { get; set; } = [];
        [JsonPropertyName("generation")]
        public string? Generation { get; set; }
    }
}
