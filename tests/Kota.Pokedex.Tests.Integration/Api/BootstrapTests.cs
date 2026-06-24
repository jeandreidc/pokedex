using System.Net.Http.Json;
using System.Text.Json;
using Kota.Pokedex.Tests.Integration.Support;

namespace Kota.Pokedex.Tests.Integration.Api;

[Collection(nameof(PokedexIntegrationCollection))]
public sealed class BootstrapTests(PokedexWebApplicationFactory factory) {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsMetadataWithPokemonCatalogCount() {
        var response = await _client.GetAsync("/api/bootstrap");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BootstrapJson>(JsonOptions);

        payload.Should().NotBeNull();
        payload!.Types.Should().NotBeEmpty();
        payload.Generations.Should().NotBeEmpty();
        payload.Abilities.TotalCount.Should().BeGreaterThan(0);
        payload.Abilities.Items.Should().NotBeEmpty();
        payload.PokemonTotalCount.Should().Be(25);
    }

    private sealed class BootstrapJson {
        public List<FilterOptionJson> Types { get; set; } = [];
        public List<FilterOptionJson> Generations { get; set; } = [];
        public PagedResultJson<FilterOptionJson> Abilities { get; set; } = new();
        public int PokemonTotalCount { get; set; }
    }

    private sealed class PagedResultJson<T> {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class FilterOptionJson {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
