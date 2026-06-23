using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Pokemon;

public class SearchPokemonQueryHandler : IRequestHandler<SearchPokemonQuery, PagedResult<PokemonSummaryDto>> {
    private const int MaxPageSize = 100;
    private readonly IPokemonIndexService _indexService;

    public SearchPokemonQueryHandler(IPokemonIndexService indexService) {
        _indexService = indexService;
    }

    public async Task<PagedResult<PokemonSummaryDto>> Handle(SearchPokemonQuery request, CancellationToken cancellationToken) {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

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

        await HydrateTypesAsync(pageItems, request.Type, cancellationToken);

        return new PagedResult<PokemonSummaryDto> {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task HydrateTypesAsync(
        List<PokemonSummaryDto> items,
        string? typeFilter,
        CancellationToken cancellationToken) {
        foreach (var item in items) {
            if (!string.IsNullOrWhiteSpace(typeFilter)) {
                item.Types = [typeFilter.ToLowerInvariant()];
                continue;
            }

            var types = await _indexService.GetTypesForPokemonAsync(item.Id, cancellationToken);
            item.Types = types.ToList();
        }
    }

    private static HashSet<int> Intersect(HashSet<int>? current, IReadOnlySet<int> next) {
        if (current is null) {
            return next.ToHashSet();
        }

        current.IntersectWith(next);
        return current;
    }
}
