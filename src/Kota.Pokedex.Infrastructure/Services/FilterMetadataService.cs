using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models.PokeApi;
using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kota.Pokedex.Infrastructure.Services;

public class FilterMetadataService : IFilterMetadataService {
    private readonly IPokeApiClient _pokeApiClient;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<FilterMetadataService> _logger;

    public FilterMetadataService(
        IPokeApiClient pokeApiClient,
        ICacheService cacheService,
        IOptions<CacheOptions> cacheOptions,
        ILogger<FilterMetadataService> logger) {
        _pokeApiClient = pokeApiClient;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<FilterOption>> GetTypesAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(CacheKeys.FilterTypes, LoadTypesAsync, cancellationToken);

    public Task<IReadOnlyList<FilterOption>> GetAbilitiesAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(CacheKeys.FilterAbilities, LoadAbilitiesAsync, cancellationToken);

    public Task<IReadOnlyList<FilterOption>> GetGenerationsAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(CacheKeys.FilterGenerations, LoadGenerationsAsync, cancellationToken);

    public async Task WarmupAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Prefetching filter metadata for dropdowns...");
        await GetTypesAsync(cancellationToken);
        await GetGenerationsAsync(cancellationToken);
        await GetAbilitiesAsync(cancellationToken);
        _logger.LogInformation("Filter metadata prefetch complete.");
    }

    private async Task<IReadOnlyList<FilterOption>> GetOrLoadAsync(
        string cacheKey,
        Func<CancellationToken, Task<List<FilterOption>>> loader,
        CancellationToken cancellationToken) {
        var cached = await _cacheService.GetAsync<List<FilterOption>>(cacheKey, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        var loaded = await loader(cancellationToken);
        await _cacheService.SetAsync(
            cacheKey,
            loaded,
            TimeSpan.FromMinutes(_cacheOptions.DefaultTtlMinutes * 7),
            cancellationToken);

        return loaded;
    }

    private async Task<List<FilterOption>> LoadTypesAsync(CancellationToken cancellationToken) {
        var results = await LoadAllPagesAsync(_pokeApiClient.GetTypeListAsync, cancellationToken);
        return results.Select(r => new FilterOption(0, r.Name, FormatDisplayName(r.Name))).ToList();
    }

    private async Task<List<FilterOption>> LoadAbilitiesAsync(CancellationToken cancellationToken) {
        var results = await LoadAllPagesAsync(_pokeApiClient.GetAbilityListAsync, cancellationToken);
        return results.Select(r => new FilterOption(0, r.Name, FormatDisplayName(r.Name))).ToList();
    }

    private async Task<List<FilterOption>> LoadGenerationsAsync(CancellationToken cancellationToken) {
        var results = await LoadAllPagesAsync(_pokeApiClient.GetGenerationListAsync, cancellationToken);
        return results.Select((r, i) => new FilterOption(
            ExtractIdFromUrl(r.Url),
            r.Name,
            FormatGenerationName(r.Name))).ToList();
    }

    private static async Task<List<PokeApiNamedResource>> LoadAllPagesAsync(
        Func<int, int, CancellationToken, Task<PokeApiListResponse>> fetchPage,
        CancellationToken cancellationToken) {
        var all = new List<PokeApiNamedResource>();
        var offset = 0;
        PokeApiListResponse page;

        do {
            page = await fetchPage(100, offset, cancellationToken);
            all.AddRange(page.Results);
            offset += 100;
        } while (page.Next is not null);

        return all;
    }

    private static int ExtractIdFromUrl(string url) {
        var segments = url.TrimEnd('/').Split('/');
        return int.Parse(segments[^1]);
    }

    private static string FormatDisplayName(string name) =>
        string.Join(' ', name.Split('-').Select(w =>
            w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));

    private static string FormatGenerationName(string name) {
        var roman = name.Replace("generation-", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        return roman switch {
            "I" => "Generation I",
            "II" => "Generation II",
            "III" => "Generation III",
            "IV" => "Generation IV",
            "V" => "Generation V",
            "VI" => "Generation VI",
            "VII" => "Generation VII",
            "VIII" => "Generation VIII",
            "IX" => "Generation IX",
            _ => name
        };
    }
}
