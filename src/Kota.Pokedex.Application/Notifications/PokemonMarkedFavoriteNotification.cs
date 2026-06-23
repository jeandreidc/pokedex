using MediatR;

namespace Kota.Pokedex.Application.Notifications;

public sealed record PokemonMarkedFavoriteNotification(
    int PokemonId,
    string PokemonName,
    string Generation) : INotification;
