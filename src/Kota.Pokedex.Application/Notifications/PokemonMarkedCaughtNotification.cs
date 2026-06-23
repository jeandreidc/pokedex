using MediatR;

namespace Kota.Pokedex.Application.Notifications;

public sealed record PokemonMarkedCaughtNotification(
    int PokemonId,
    string PokemonName,
    string Generation) : INotification;
