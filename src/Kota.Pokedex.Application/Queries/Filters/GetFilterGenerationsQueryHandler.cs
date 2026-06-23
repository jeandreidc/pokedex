using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Filters;

public class GetFilterGenerationsQueryHandler : IRequestHandler<GetFilterGenerationsQuery, IReadOnlyList<FilterOptionDto>> {
    private readonly IFilterMetadataService _filterMetadata;

    public GetFilterGenerationsQueryHandler(IFilterMetadataService filterMetadata) {
        _filterMetadata = filterMetadata;
    }

    public async Task<IReadOnlyList<FilterOptionDto>> Handle(GetFilterGenerationsQuery request, CancellationToken cancellationToken) {
        var generations = await _filterMetadata.GetGenerationsAsync(cancellationToken);
        return generations.Select(g => new FilterOptionDto {
            Id = g.Id,
            Name = g.Name,
            DisplayName = g.DisplayName
        }).ToList();
    }
}
