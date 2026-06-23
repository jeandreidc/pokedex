using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Pokemon;

public class GetPokemonByIdQueryHandler : IRequestHandler<GetPokemonByIdQuery, PokemonSummaryDto?> {
    private readonly IPokemonIndexService _indexService;

    public GetPokemonByIdQueryHandler(IPokemonIndexService indexService) {
        _indexService = indexService;
    }

    public async Task<PokemonSummaryDto?> Handle(GetPokemonByIdQuery request, CancellationToken cancellationToken) {
        var index = await _indexService.GetIndexAsync(cancellationToken);
        var entry = index.FirstOrDefault(e =>
            e.Name.Equals(request.IdOrName, StringComparison.OrdinalIgnoreCase) ||
            e.Id.ToString() == request.IdOrName);

        if (entry is null && int.TryParse(request.IdOrName, out var id)) {
            entry = await _indexService.GetEntryAsync(id, cancellationToken);
        }

        if (entry is null) {
            return null;
        }

        var types = await _indexService.GetTypesForPokemonAsync(entry.Id, cancellationToken);
        return new PokemonSummaryDto {
            Id = entry.Id,
            Name = entry.Name,
            SpriteUrl = entry.SpriteUrl,
            Types = types.ToList()
        };
    }
}
