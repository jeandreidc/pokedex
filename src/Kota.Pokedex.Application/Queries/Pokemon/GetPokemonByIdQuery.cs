using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Pokemon;

public record GetPokemonByIdQuery(string IdOrName) : IRequest<PokemonSummaryDto?>;
