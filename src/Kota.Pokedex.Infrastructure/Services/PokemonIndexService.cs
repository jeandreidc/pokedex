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

    public async Task<PokemonCardDetails> GetPokemonCardDetailsAsync(int id, CancellationToken cancellationToken = default) {
        var key = CacheKeys.PokemonCard(id);
        var cached = await _cacheService.GetAsync<PokemonCardDetails>(key, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var detail = await _pokeApiClient.GetPokemonAsync(id.ToString(), cancellationToken);
        var generationMap = await GetPokemonGenerationMapAsync(cancellationToken);

        var cardDetails = new PokemonCardDetails {
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

        await _cacheService.SetAsync(key, cardDetails, TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes), cancellationToken);
        return cardDetails;
    }

    public Task<PokemonCardDetails?> GetCachedCardDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        _cacheService.GetAsync<PokemonCardDetails>(CacheKeys.PokemonCard(id), cancellationToken);

    private async Task<IReadOnlyDictionary<int, string>> GetPokemonGenerationMapAsync(CancellationToken cancellationToken) {
        var cached = await _cacheService.GetAsync<Dictionary<int, string>>(CacheKeys.PokemonGenerationMap, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var index = await GetIndexAsync(cancellationToken);
        var nameToId = index.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<int, string>();
        var offset = 0;
        PokeApiListResponse page;

        do {
            page = await _pokeApiClient.GetGenerationListAsync(100, offset, cancellationToken);
            foreach (var generation in page.Results) {
                var detail = await _pokeApiClient.GetGenerationAsync(generation.Name, cancellationToken);
                var displayName = FormatGenerationName(detail.Name);
                foreach (var species in detail.PokemonSpecies) {
                    if (nameToId.TryGetValue(species.Name, out var pokemonId)) {
                        map[pokemonId] = displayName;
                    }
                }
            }

            offset += 100;
        } while (page.Next is not null);

        await _cacheService.SetAsync(
            CacheKeys.PokemonGenerationMap,
            map,
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes * 7),
            cancellationToken);

        return map;
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
