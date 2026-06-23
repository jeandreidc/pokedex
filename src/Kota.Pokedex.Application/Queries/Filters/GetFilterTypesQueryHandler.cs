using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Filters;

public class GetFilterTypesQueryHandler : IRequestHandler<GetFilterTypesQuery, IReadOnlyList<FilterOptionDto>> {
    private readonly IFilterMetadataService _filterMetadata;

    public GetFilterTypesQueryHandler(IFilterMetadataService filterMetadata) {
        _filterMetadata = filterMetadata;
    }

    public async Task<IReadOnlyList<FilterOptionDto>> Handle(GetFilterTypesQuery request, CancellationToken cancellationToken) {
        var types = await _filterMetadata.GetTypesAsync(cancellationToken);
        return types.Select(ToDto).ToList();
    }

    private static FilterOptionDto ToDto(FilterOption option) => new() {
        Id = option.Id,
        Name = option.Name,
        DisplayName = option.DisplayName
    };
}
