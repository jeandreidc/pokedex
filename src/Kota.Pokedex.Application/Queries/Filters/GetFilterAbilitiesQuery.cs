using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Filters;

public record GetFilterAbilitiesQuery : IRequest<PagedResult<FilterOptionDto>> {
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
