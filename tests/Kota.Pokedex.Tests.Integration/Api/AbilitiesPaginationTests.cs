using System.Net.Http.Json;
using System.Text.Json;
using Kota.Pokedex.Tests.Integration.Fixtures;
using Kota.Pokedex.Tests.Integration.Support;

namespace Kota.Pokedex.Tests.Integration.Api;

[Collection(nameof(PokedexIntegrationCollection))]
public sealed class AbilitiesPaginationTests(PokedexWebApplicationFactory factory) {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAbilities_FirstPage_ReturnsCorrectPaginationMetadata() {
        var response = await _client.GetAsync("/api/filters/abilities?page=1&pageSize=5");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<FilterOptionJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(5);
        page.TotalCount.Should().Be(IntegrationPokeApiFixtures.TotalAbilities);
        page.TotalPages.Should().Be(3);
        page.Items.Should().HaveCount(5);
        page.Items.Select(i => i.Name).Should().Equal(
            "ability-10", "ability-11", "ability-12", "ability-13", "ability-14");
    }

    [Fact]
    public async Task GetAbilities_SecondPage_ReturnsNextBatch() {
        var response = await _client.GetAsync("/api/filters/abilities?page=2&pageSize=5");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<FilterOptionJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(2);
        page.Items.Should().HaveCount(5);
        page.Items.Select(i => i.Name).Should().Equal(
            "ability-15", "ability-6", "ability-7", "ability-8", "ability-9");
    }

    [Fact]
    public async Task GetAbilities_WithSearch_ReturnsFilteredPage() {
        var response = await _client.GetAsync("/api/filters/abilities?search=over&page=1&pageSize=10");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<FilterOptionJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(1);
        page.TotalPages.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].Name.Should().Be("overgrow");
        page.Items[0].DisplayName.Should().Be("Overgrow");
    }

    [Fact]
    public async Task GetAbilities_PageZero_NormalizesToFirstPage() {
        var response = await _client.GetAsync("/api/filters/abilities?page=0&pageSize=5");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResultJson<FilterOptionJson>>(JsonOptions);

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.Items.Should().HaveCount(5);
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
