using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        var response = await _client.GetAsync("/api/pokemon?page=1&pageSize=5");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(5);
        page.TotalCount.Should().Be(IntegrationPokeApiFixtures.TotalPokemon);
        page.TotalPages.Should().Be(5);
        page.Items.Should().HaveCount(5);
        page.Items.Select(i => i.Id).Should().Equal(1, 2, 3, 4, 5);
        page.Items.Should().AllSatisfy(i => {
            i.Name.Should().NotBeNullOrWhiteSpace();
            i.SpriteUrl.Should().NotBeNullOrWhiteSpace();
            i.Types.Should().NotBeEmpty();
            i.Abilities.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task Get_SecondPage_ReturnsNextBatch() {
        var response = await _client.GetAsync("/api/pokemon?page=2&pageSize=5");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(2);
        page.Items.Should().HaveCount(5);
        page.Items.Select(i => i.Id).Should().Equal(6, 7, 8, 9, 10);
    }

    [Fact]
    public async Task Get_PageZero_NormalizesToFirstPage() {
        var response = await _client.GetAsync("/api/pokemon?page=0&pageSize=5");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.Items.Select(i => i.Id).Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task Get_PageSizeOverMax_ClampsTo100() {
        var response = await _client.GetAsync("/api/pokemon?page=1&pageSize=500");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.PageSize.Should().Be(100);
        page.TotalCount.Should().Be(IntegrationPokeApiFixtures.TotalPokemon);
        page.Items.Should().HaveCount(IntegrationPokeApiFixtures.TotalPokemon);
    }

    [Fact]
    public async Task Get_WithSearchAndPagination_ReturnsFilteredPage() {
        var response = await _client.GetAsync("/api/pokemon?search=char&page=1&pageSize=2");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<PokemonSummaryJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Select(i => i.Name).Should().Equal("charmander", "charmeleon");
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
