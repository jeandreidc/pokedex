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
    public async Task Get_ReturnsFirstPageWithTotalPokemonCount() {
        var response = await _client.GetAsync("/api/bootstrap?pageSize=5");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BootstrapJson>(JsonOptions);

        payload.Should().NotBeNull();
        payload!.Types.Should().NotBeEmpty();
        payload.Generations.Should().NotBeEmpty();
        payload.Abilities.TotalCount.Should().BeGreaterThan(0);
        payload.Abilities.Items.Should().NotBeEmpty();
        payload.Pokemon.TotalCount.Should().Be(25);
        payload.Pokemon.TotalPages.Should().Be(5);
        payload.Pokemon.Items.Should().HaveCount(5);
        payload.Pokemon.Items[0].Types.Should().NotBeEmpty();
    }

    private sealed class BootstrapJson {
        public List<FilterOptionJson> Types { get; set; } = [];
        public List<FilterOptionJson> Generations { get; set; } = [];
        public PagedResultJson<FilterOptionJson> Abilities { get; set; } = new();
        public PagedResultJson<PokemonSummaryJson> Pokemon { get; set; } = new();
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

    private sealed class PokemonSummaryJson {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<string> Types { get; set; } = [];
    }
}
