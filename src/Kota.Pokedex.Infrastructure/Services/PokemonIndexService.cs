using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models;
using Kota.Pokedex.Core.Models.PokeApi;
using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kota.Pokedex.Infrastructure.Services;

public class PokemonIndexService : IPokemonIndexService {
    private readonly IPokeApiClient _pokeApiClient;
    private readonly ICacheService _cacheService;
    private readonly PokeApiOptions _pokeApiOptions;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<PokemonIndexService> _logger;

    public PokemonIndexService(
        IPokeApiClient pokeApiClient,
        ICacheService cacheService,
        IOptions<PokeApiOptions> pokeApiOptions,
        IOptions<CacheOptions> cacheOptions,
        ILogger<PokemonIndexService> logger) {
        _pokeApiClient = pokeApiClient;
        _cacheService = cacheService;
        _pokeApiOptions = pokeApiOptions.Value;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task WarmupAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Starting Pokemon index warmup...");
        await GetIndexAsync(cancellationToken);
        _logger.LogInformation("Pokemon index warmup complete.");
    }

    public async Task<IReadOnlyList<PokemonIndexEntry>> GetIndexAsync(CancellationToken cancellationToken = default) {
        var cached = await _cacheService.GetAsync<List<PokemonIndexEntry>>(CacheKeys.PokemonIndex, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var entries = new List<PokemonIndexEntry>();
        var offset = 0;
        PokeApiListResponse page;

        do {
            page = await _pokeApiClient.GetPokemonListAsync(_pokeApiOptions.PageFetchLimit, offset, cancellationToken);
            entries.AddRange(page.Results.Select(r => new PokemonIndexEntry {
                Id = ExtractIdFromUrl(r.Url),
                Name = r.Name,
                SpriteUrl = BuildSpriteUrl(ExtractIdFromUrl(r.Url))
            }));
            offset += _pokeApiOptions.PageFetchLimit;
        } while (page.Next is not null);

        await _cacheService.SetAsync(
            CacheKeys.PokemonIndex,
            entries,
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes),
            cancellationToken);

        return entries;
    }

    public async Task<IReadOnlySet<int>> GetPokemonIdsByTypeAsync(string type, CancellationToken cancellationToken = default) {
        var key = CacheKeys.Type(type);
        var cached = await _cacheService.GetAsync<HashSet<int>>(key, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var detail = await _pokeApiClient.GetTypeAsync(type, cancellationToken);
        var ids = detail.Pokemon
            .Select(p => ExtractIdFromUrl(p.Pokemon.Url))
            .ToHashSet();

        await _cacheService.SetAsync(key, ids, TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes), cancellationToken);
        return ids;
    }

    public async Task<IReadOnlySet<int>> GetPokemonIdsByAbilityAsync(string ability, CancellationToken cancellationToken = default) {
        var key = CacheKeys.Ability(ability);
        var cached = await _cacheService.GetAsync<HashSet<int>>(key, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var detail = await _pokeApiClient.GetAbilityAsync(ability, cancellationToken);
        var ids = detail.Pokemon
            .Select(p => ExtractIdFromUrl(p.Pokemon.Url))
            .ToHashSet();

        await _cacheService.SetAsync(key, ids, TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes), cancellationToken);
        return ids;
    }

    public async Task<IReadOnlySet<int>> GetPokemonIdsByGenerationAsync(string generation, CancellationToken cancellationToken = default) {
        var key = CacheKeys.Generation(generation);
        var cached = await _cacheService.GetAsync<HashSet<int>>(key, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var detail = await _pokeApiClient.GetGenerationAsync(generation, cancellationToken);
        var index = await GetIndexAsync(cancellationToken);
        var nameToId = index.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);

        var ids = new HashSet<int>();
        foreach (var species in detail.PokemonSpecies) {
            if (nameToId.TryGetValue(species.Name, out var id)) {
                ids.Add(id);
            }
        }

        await _cacheService.SetAsync(key, ids, TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes), cancellationToken);
        return ids;
    }

    public async Task<PokemonIndexEntry?> GetEntryAsync(int id, CancellationToken cancellationToken = default) {
        var index = await GetIndexAsync(cancellationToken);
        return index.FirstOrDefault(e => e.Id == id);
    }

    public async Task<IReadOnlyList<string>> GetTypesForPokemonAsync(int id, CancellationToken cancellationToken = default) {
        var key = CacheKeys.PokemonDetail(id);
        var cached = await _cacheService.GetAsync<List<string>>(key, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var detail = await _pokeApiClient.GetPokemonAsync(id.ToString(), cancellationToken);
        var types = detail.Types.Select(t => t.Type.Name).ToList();
        await _cacheService.SetAsync(key, types, TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes), cancellationToken);
        return types;
    }

    private static int ExtractIdFromUrl(string url) {
        var segments = url.TrimEnd('/').Split('/');
        return int.Parse(segments[^1]);
    }

    private static string BuildSpriteUrl(int id) =>
        $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png";
}
