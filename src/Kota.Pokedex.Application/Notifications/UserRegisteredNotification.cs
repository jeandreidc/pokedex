using MediatR;

namespace Kota.Pokedex.Application.Notifications;

public sealed record UserRegisteredNotification(string Username) : INotification;
