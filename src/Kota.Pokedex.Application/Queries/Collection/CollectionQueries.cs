using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Collection;

public record GetCollectionQuery(bool? FavoritesOnly, bool? CaughtOnly) : IRequest<IReadOnlyList<CollectionEntryDto>>;

public record GetCollectionEntryQuery(int PokemonId) : IRequest<CollectionEntryDto?>;

public record GetCollectionStatsQuery : IRequest<CollectionStatsDto>;
