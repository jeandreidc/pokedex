using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Core.Entities;
using Kota.Pokedex.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Kota.Pokedex.Application.Commands.Auth;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto> {
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public RegisterUserCommandHandler(IUserRepository userRepository, IJwtTokenService jwtTokenService) {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken) {
        var existing = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null) {
            throw new InvalidOperationException("Username is already taken.");
        }

        var user = new User {
            Id = Guid.NewGuid(),
            Username = request.Username,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.GenerateToken(user);
        return new AuthResponseDto(token.Token, user.Username, token.ExpiresAtUtc);
    }
}

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResponseDto> {
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public LoginUserCommandHandler(IUserRepository userRepository, IJwtTokenService jwtTokenService) {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(LoginUserCommand request, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null) {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var token = _jwtTokenService.GenerateToken(user);
        return new AuthResponseDto(token.Token, user.Username, token.ExpiresAtUtc);
    }
}
