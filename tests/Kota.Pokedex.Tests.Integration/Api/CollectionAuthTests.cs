using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Tests.Integration.Support;

namespace Kota.Pokedex.Tests.Integration.Api;

[Collection(nameof(PokedexIntegrationCollection))]
public sealed class CollectionAuthTests {
    private readonly HttpClient _client;
    private readonly PokedexWebApplicationFactory _factory;

    public CollectionAuthTests(PokedexWebApplicationFactory factory) {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CollectionEndpoints_WithoutToken_ReturnUnauthorized() {
        var response = await _client.GetAsync("/api/collection");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterLoginAndCollection_WorksForAuthenticatedUser() {
        var username = $"user_{Guid.NewGuid():N}"[..16];
        const string password = "password123";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new {
            username,
            password
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));

        using var authedClient = CreateAuthedClient(auth.Token);

        var updateResponse = await authedClient.PutAsJsonAsync("/api/collection/25", new {
            isFavorite = true,
            isCaught = true
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var entry = await updateResponse.Content.ReadFromJsonAsync<CollectionEntryDto>();
        Assert.NotNull(entry);
        Assert.Equal(25, entry!.PokemonId);
        Assert.True(entry.IsFavorite);
        Assert.True(entry.IsCaught);

        var listResponse = await authedClient.GetAsync("/api/collection?favoritesOnly=true");
        listResponse.EnsureSuccessStatusCode();
        var favorites = await listResponse.Content.ReadFromJsonAsync<List<CollectionEntryDto>>();
        Assert.NotNull(favorites);
        Assert.Contains(favorites!, e => e.PokemonId == 25);

        var statsResponse = await authedClient.GetAsync("/api/collection/stats");
        statsResponse.EnsureSuccessStatusCode();
        var stats = await statsResponse.Content.ReadFromJsonAsync<CollectionStatsDto>();
        Assert.NotNull(stats);
        Assert.Equal(1, stats!.TotalCaught);
        Assert.Equal(1, stats.TotalFavorites);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict() {
        var username = $"dup_{Guid.NewGuid():N}"[..16];
        const string password = "password123";

        var first = await _client.PostAsJsonAsync("/api/auth/register", new { username, password });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/auth/register", new { username, password });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private HttpClient CreateAuthedClient(string token) {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
