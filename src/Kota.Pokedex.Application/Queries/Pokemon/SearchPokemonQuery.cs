using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Pokemon;

public record SearchPokemonQuery : IRequest<PagedResult<PokemonSummaryDto>> {
    public string? Search { get; init; }
    public string? Type { get; init; }
    public string? Ability { get; init; }
    public string? Generation { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool CacheOnlyHydration { get; init; }
}
