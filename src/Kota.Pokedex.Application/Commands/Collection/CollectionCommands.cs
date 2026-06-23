using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Commands.Collection;

public record UpdateCollectionEntryCommand(int PokemonId, bool? IsCaught, bool? IsFavorite) : IRequest<CollectionEntryDto>;

public record RemoveCollectionEntryCommand(int PokemonId) : IRequest<Unit>;
