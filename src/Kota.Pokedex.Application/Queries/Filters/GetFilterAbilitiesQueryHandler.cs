using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Filters;

public class GetFilterAbilitiesQueryHandler : IRequestHandler<GetFilterAbilitiesQuery, PagedResult<FilterOptionDto>> {
    private readonly IFilterMetadataService _filterMetadata;

    public GetFilterAbilitiesQueryHandler(IFilterMetadataService filterMetadata) {
        _filterMetadata = filterMetadata;
    }

    public async Task<PagedResult<FilterOptionDto>> Handle(GetFilterAbilitiesQuery request, CancellationToken cancellationToken) {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var abilities = await _filterMetadata.GetAbilitiesAsync(cancellationToken);
        IEnumerable<FilterOption> filtered = abilities.OrderBy(a => a.Name);

        if (!string.IsNullOrWhiteSpace(request.Search)) {
            var term = request.Search.Trim();
            filtered = filtered.Where(a =>
                a.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new FilterOptionDto {
                Id = a.Id,
                Name = a.Name,
                DisplayName = a.DisplayName
            })
            .ToList();

        return new PagedResult<FilterOptionDto> {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = list.Count
        };
    }
}
