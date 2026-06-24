using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Pokemon;

public class SearchPokemonQueryHandler : IRequestHandler<SearchPokemonQuery, PagedResult<PokemonSummaryDto>> {
    private readonly IPokemonIndexService _indexService;

    public SearchPokemonQueryHandler(IPokemonIndexService indexService) {
        _indexService = indexService;
    }

    public async Task<PagedResult<PokemonSummaryDto>> Handle(SearchPokemonQuery request, CancellationToken cancellationToken) {
        var page = Math.Max(1, request.Page);
        var pageSize = PokemonPagination.CatalogPageSize;

        var index = await _indexService.GetIndexAsync(cancellationToken);
        IEnumerable<PokemonSummaryDto> candidates = index.Select(e => new PokemonSummaryDto {
            Id = e.Id,
            Name = e.Name,
            SpriteUrl = e.SpriteUrl,
            Types = []
        });

        HashSet<int>? filterIds = null;

        if (!string.IsNullOrWhiteSpace(request.Type)) {
            var typeIds = await _indexService.GetPokemonIdsByTypeAsync(request.Type, cancellationToken);
            filterIds = Intersect(filterIds, typeIds);
        }

        if (!string.IsNullOrWhiteSpace(request.Ability)) {
            var abilityIds = await _indexService.GetPokemonIdsByAbilityAsync(request.Ability, cancellationToken);
            filterIds = Intersect(filterIds, abilityIds);
        }

        if (!string.IsNullOrWhiteSpace(request.Generation)) {
            var generationIds = await _indexService.GetPokemonIdsByGenerationAsync(request.Generation, cancellationToken);
            filterIds = Intersect(filterIds, generationIds);
        }

        if (filterIds is not null) {
            candidates = candidates.Where(c => filterIds.Contains(c.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Search)) {
            var term = request.Search.Trim();
            candidates = candidates.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = candidates.OrderBy(c => c.Id).ToList();
        var totalCount = ordered.Count;
        var pageItems = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        await HydrateCardDetailsAsync(pageItems, request.CacheOnlyHydration, cancellationToken);

        return new PagedResult<PokemonSummaryDto> {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task HydrateCardDetailsAsync(
        List<PokemonSummaryDto> items,
        bool cacheOnly,
        CancellationToken cancellationToken) {
        if (!cacheOnly) {
            await HydrateFromApiAsync(items, cancellationToken);
            return;
        }

        var missing = new List<PokemonSummaryDto>();

        foreach (var item in items) {
            var cached = await _indexService.GetCachedCardDetailsAsync(item.Id, cancellationToken);
            if (cached is null) {
                missing.Add(item);
                continue;
            }

            item.Types = cached.Types.ToList();
            item.Abilities = cached.Abilities.ToList();
            item.Generation = cached.Generation;
        }

        if (missing.Count > 0) {
            await HydrateFromApiAsync(missing, cancellationToken);
        }
    }

    private async Task HydrateFromApiAsync(List<PokemonSummaryDto> items, CancellationToken cancellationToken) {
        var hydrateTasks = items.Select(async item => {
            var details = await _indexService.GetPokemonCardDetailsAsync(item.Id, cancellationToken);
            item.Types = details.Types.ToList();
            item.Abilities = details.Abilities.ToList();
            item.Generation = details.Generation;
        });

        await Task.WhenAll(hydrateTasks);
    }

    private static HashSet<int> Intersect(HashSet<int>? current, IReadOnlySet<int> next) {
        if (current is null) {
            return next.ToHashSet();
        }

        current.IntersectWith(next);
        return current;
    }
}
