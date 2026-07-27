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
        _logger.LogInformation("Starting Pokemon index warmup");
        var index = await GetIndexAsync(cancellationToken);
        await GetPokemonGenerationMapAsync(cancellationToken);
        _logger.LogInformation("Pokemon index warmup complete with {EntryCount} entries", index.Count);
    }

    public async Task PrefetchFirstPageCardDetailsAsync(int pageSize, CancellationToken cancellationToken = default) {
        var index = await GetIndexAsync(cancellationToken);
        var ids = index.OrderBy(e => e.Id).Take(pageSize).Select(e => e.Id).ToList();
        if (ids.Count == 0) {
            return;
        }

        _logger.LogInformation("Prefetching card details for first page ({Count} Pokémon)", ids.Count);
        var tasks = ids.Select(id => GetPokemonCardDetailsAsync(id, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public Task<IReadOnlyList<PokemonIndexEntry>> GetIndexAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateListAsync(
            CacheKeys.PokemonIndex,
            async ct => {
                var entries = new List<PokemonIndexEntry>();
                var offset = 0;
                PokeApiListResponse page;

                do {
                    page = await _pokeApiClient.GetPokemonListAsync(_pokeApiOptions.PageFetchLimit, offset, ct);
                    entries.AddRange(page.Results.Select(r => new PokemonIndexEntry {
                        Id = ExtractIdFromUrl(r.Url),
                        Name = r.Name,
                        SpriteUrl = BuildSpriteUrl(ExtractIdFromUrl(r.Url))
                    }));
                    offset += _pokeApiOptions.PageFetchLimit;
                } while (page.Next is not null);

                await _cacheService.SetAsync(
                    CacheKeys.PokemonIndexMap,
                    entries.ToDictionary(e => e.Id),
                    TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes),
                    ct);

                return entries;
            },
            cancellationToken);

    public Task<IReadOnlySet<int>> GetPokemonIdsByTypeAsync(string type, CancellationToken cancellationToken = default) =>
        GetOrCreateSetAsync(
            CacheKeys.Type(type),
            async ct => {
                var detail = await _pokeApiClient.GetTypeAsync(type, ct);
                return detail.Pokemon.Select(p => ExtractIdFromUrl(p.Pokemon.Url)).ToHashSet();
            },
            cancellationToken);

    public Task<IReadOnlySet<int>> GetPokemonIdsByAbilityAsync(string ability, CancellationToken cancellationToken = default) =>
        GetOrCreateSetAsync(
            CacheKeys.Ability(ability),
            async ct => {
                var detail = await _pokeApiClient.GetAbilityAsync(ability, ct);
                return detail.Pokemon.Select(p => ExtractIdFromUrl(p.Pokemon.Url)).ToHashSet();
            },
            cancellationToken);

    public Task<IReadOnlySet<int>> GetPokemonIdsByGenerationAsync(string generation, CancellationToken cancellationToken = default) =>
        GetOrCreateSetAsync(
            CacheKeys.Generation(generation),
            async ct => {
                var detail = await _pokeApiClient.GetGenerationAsync(generation, ct);
                var index = await GetIndexAsync(ct);
                var nameToId = index.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);

                var ids = new HashSet<int>();
                foreach (var species in detail.PokemonSpecies) {
                    if (nameToId.TryGetValue(species.Name, out var id)) {
                        ids.Add(id);
                    }
                }

                return ids;
            },
            cancellationToken);

    public async Task<PokemonIndexEntry?> GetEntryAsync(int id, CancellationToken cancellationToken = default) {
        var map = await GetIndexMapAsync(cancellationToken);
        return map.TryGetValue(id, out var entry) ? entry : null;
    }

    private async Task<IReadOnlyDictionary<int, PokemonIndexEntry>> GetIndexMapAsync(CancellationToken cancellationToken) {
        var map = await _cacheService.GetOrCreateAsync(
            CacheKeys.PokemonIndexMap,
            async ct => {
                var index = await GetIndexAsync(ct);
                return index.ToDictionary(e => e.Id);
            },
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes),
            cancellationToken);
        return map;
    }

    public Task<PokemonCardDetails> GetPokemonCardDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        _cacheService.GetOrCreateAsync(
            CacheKeys.PokemonCard(id),
            async ct => {
                var detail = await _pokeApiClient.GetPokemonAsync(id.ToString(), ct);
                var generationMap = await GetPokemonGenerationMapAsync(ct);

                return new PokemonCardDetails {
                    Types = detail.Types
                        .OrderBy(t => t.Slot)
                        .Select(t => t.Type.Name)
                        .ToList(),
                    Abilities = detail.Abilities
                        .OrderBy(a => a.Slot)
                        .Select(a => FormatDisplayName(a.Ability.Name))
                        .ToList(),
                    Generation = generationMap.TryGetValue(id, out var generation) ? generation : null
                };
            },
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes),
            cancellationToken);

    public Task<PokemonCardDetails?> GetCachedCardDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        _cacheService.GetAsync<PokemonCardDetails>(CacheKeys.PokemonCard(id), cancellationToken);

    private async Task<IReadOnlyDictionary<int, string>> GetPokemonGenerationMapAsync(CancellationToken cancellationToken) {
        var map = await _cacheService.GetOrCreateAsync(
            CacheKeys.PokemonGenerationMap,
            async ct => {
                var index = await GetIndexAsync(ct);
                var nameToId = index.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);
                var result = new Dictionary<int, string>();
                var offset = 0;
                PokeApiListResponse page;

                do {
                    page = await _pokeApiClient.GetGenerationListAsync(100, offset, ct);
                    foreach (var generation in page.Results) {
                        var detail = await _pokeApiClient.GetGenerationAsync(generation.Name, ct);
                        var displayName = FormatGenerationName(detail.Name);
                        foreach (var species in detail.PokemonSpecies) {
                            if (nameToId.TryGetValue(species.Name, out var pokemonId)) {
                                result[pokemonId] = displayName;
                            }
                        }
                    }

                    offset += 100;
                } while (page.Next is not null);

                return result;
            },
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes * 7),
            cancellationToken);
        return map;
    }

    private async Task<IReadOnlyList<PokemonIndexEntry>> GetOrCreateListAsync(
        string key,
        Func<CancellationToken, Task<List<PokemonIndexEntry>>> factory,
        CancellationToken cancellationToken) {
        var list = await _cacheService.GetOrCreateAsync(
            key,
            factory,
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes),
            cancellationToken);
        return list;
    }

    private async Task<IReadOnlySet<int>> GetOrCreateSetAsync(
        string key,
        Func<CancellationToken, Task<HashSet<int>>> factory,
        CancellationToken cancellationToken) {
        var set = await _cacheService.GetOrCreateAsync(
            key,
            factory,
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes),
            cancellationToken);
        return set;
    }

    private static string FormatDisplayName(string name) =>
        string.Join(' ', name.Split('-').Select(w =>
            w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));

    private static string FormatGenerationName(string name) {
        var roman = name.Replace("generation-", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        return roman switch {
            "I" or "II" or "III" or "IV" or "V" or "VI" or "VII" or "VIII" or "IX" => roman,
            _ => name
        };
    }

    private static int ExtractIdFromUrl(string url) {
        var segments = url.TrimEnd('/').Split('/');
        return int.Parse(segments[^1]);
    }

    private static string BuildSpriteUrl(int id) =>
        $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png";
}
