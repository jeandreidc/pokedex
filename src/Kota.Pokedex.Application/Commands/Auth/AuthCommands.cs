using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Commands.Auth;

public record RegisterUserCommand(string Username, string Password) : IRequest<AuthResponseDto>;

public record LoginUserCommand(string Username, string Password) : IRequest<AuthResponseDto>;
