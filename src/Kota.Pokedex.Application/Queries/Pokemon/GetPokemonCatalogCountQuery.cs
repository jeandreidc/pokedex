using MediatR;

namespace Kota.Pokedex.Application.Queries.Pokemon;

public record GetPokemonCatalogCountQuery : IRequest<int> {
    public string? Search { get; init; }
    public string? Type { get; init; }
    public string? Ability { get; init; }
    public string? Generation { get; init; }
}
