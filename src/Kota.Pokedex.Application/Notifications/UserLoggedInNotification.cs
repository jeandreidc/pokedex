using MediatR;

namespace Kota.Pokedex.Application.Notifications;

public sealed record UserLoggedInNotification(string Username) : INotification;
