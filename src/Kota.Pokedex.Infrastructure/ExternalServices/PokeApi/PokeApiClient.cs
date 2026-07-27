using System.Text.Json;
using Kota.Pokedex.Core.Exceptions;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models.PokeApi;
using Microsoft.Extensions.Logging;

namespace Kota.Pokedex.Infrastructure.ExternalServices.PokeApi;

public class PokeApiClient : IPokeApiClient {
    public const string HttpClientName = "PokeApi";

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PokeApiClient> _logger;

    public PokeApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<PokeApiClient> logger) {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<PokeApiListResponse> GetPokemonListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiListResponse>($"pokemon?limit={limit}&offset={offset}", cancellationToken);

    public Task<PokeApiListResponse> GetTypeListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiListResponse>($"type?limit={limit}&offset={offset}", cancellationToken);

    public Task<PokeApiListResponse> GetAbilityListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiListResponse>($"ability?limit={limit}&offset={offset}", cancellationToken);

    public Task<PokeApiListResponse> GetGenerationListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiListResponse>($"generation?limit={limit}&offset={offset}", cancellationToken);

    public Task<PokeApiTypeDetail> GetTypeAsync(string nameOrId, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiTypeDetail>($"type/{nameOrId.ToLowerInvariant()}", cancellationToken);

    public Task<PokeApiAbilityDetail> GetAbilityAsync(string nameOrId, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiAbilityDetail>($"ability/{nameOrId.ToLowerInvariant()}", cancellationToken);

    public Task<PokeApiGenerationDetail> GetGenerationAsync(string nameOrId, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiGenerationDetail>($"generation/{NormalizeGeneration(nameOrId)}", cancellationToken);

    public Task<PokeApiPokemonDetail> GetPokemonAsync(string nameOrId, CancellationToken cancellationToken = default) =>
        GetAsync<PokeApiPokemonDetail>($"pokemon/{nameOrId.ToLowerInvariant()}", cancellationToken);

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) {
        _logger.LogDebug("PokeAPI GET {Path}", path);
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (!response.IsSuccessStatusCode) {
            throw new PokeApiException(
                $"PokeAPI request failed for '{path}' with status {(int)response.StatusCode}.",
                (int)response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        if (result is null) {
            throw new PokeApiException($"PokeAPI returned empty response for '{path}'.");
        }

        return result;
    }

    private static string NormalizeGeneration(string nameOrId) {
        if (int.TryParse(nameOrId, out var id)) {
            return id switch {
                1 => "generation-i",
                2 => "generation-ii",
                3 => "generation-iii",
                4 => "generation-iv",
                5 => "generation-v",
                6 => "generation-vi",
                7 => "generation-vii",
                8 => "generation-viii",
                9 => "generation-ix",
                _ => nameOrId
            };
        }

        return nameOrId.ToLowerInvariant();
    }
}
