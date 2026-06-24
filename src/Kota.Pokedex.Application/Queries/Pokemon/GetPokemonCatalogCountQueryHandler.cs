using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Pokemon;

public class GetPokemonCatalogCountQueryHandler : IRequestHandler<GetPokemonCatalogCountQuery, int> {
    private readonly IPokemonIndexService _indexService;

    public GetPokemonCatalogCountQueryHandler(IPokemonIndexService indexService) {
        _indexService = indexService;
    }

    public async Task<int> Handle(GetPokemonCatalogCountQuery request, CancellationToken cancellationToken) {
        var index = await _indexService.GetIndexAsync(cancellationToken);
        IEnumerable<int> candidateIds = index.Select(e => e.Id);

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
            candidateIds = candidateIds.Where(filterIds.Contains);
        }

        if (!string.IsNullOrWhiteSpace(request.Search)) {
            var term = request.Search.Trim();
            var nameToId = index.ToDictionary(e => e.Id, e => e.Name);
            candidateIds = candidateIds.Where(id =>
                nameToId.TryGetValue(id, out var name) &&
                name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return candidateIds.Count();
    }

    private static HashSet<int> Intersect(HashSet<int>? current, IReadOnlySet<int> next) {
        if (current is null) {
            return next.ToHashSet();
        }

        current.IntersectWith(next);
        return current;
    }
}
