using Kota.Pokedex.Application.Interfaces;
using Kota.Pokedex.Application.Notifications;
using MediatR;

namespace Kota.Pokedex.Application.Notifications.Handlers;

public sealed class PokemonMarkedFavoriteMetricsHandler(IPokedexMetricsService metrics)
    : INotificationHandler<PokemonMarkedFavoriteNotification> {
    public Task Handle(PokemonMarkedFavoriteNotification notification, CancellationToken cancellationToken) {
        metrics.RecordPokemonFavorited(notification.PokemonName, notification.Generation);
        return Task.CompletedTask;
    }
}

public sealed class PokemonMarkedCaughtMetricsHandler(IPokedexMetricsService metrics)
    : INotificationHandler<PokemonMarkedCaughtNotification> {
    public Task Handle(PokemonMarkedCaughtNotification notification, CancellationToken cancellationToken) {
        metrics.RecordPokemonCaught(notification.PokemonName, notification.Generation);
        return Task.CompletedTask;
    }
}

public sealed class UserRegisteredMetricsHandler(IPokedexMetricsService metrics)
    : INotificationHandler<UserRegisteredNotification> {
    public Task Handle(UserRegisteredNotification notification, CancellationToken cancellationToken) {
        metrics.RecordUserRegistered();
        return Task.CompletedTask;
    }
}

public sealed class UserLoggedInMetricsHandler(IPokedexMetricsService metrics)
    : INotificationHandler<UserLoggedInNotification> {
    public Task Handle(UserLoggedInNotification notification, CancellationToken cancellationToken) {
        metrics.RecordUserLoggedIn();
        return Task.CompletedTask;
    }
}
