using Kota.Pokedex.Core.Entities;

namespace Kota.Pokedex.Core.Interfaces;

public interface IJwtTokenService {
    AuthTokenResult GenerateToken(User user);
}

public record AuthTokenResult(string Token, DateTime ExpiresAtUtc);
