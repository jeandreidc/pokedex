using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kota.Pokedex.Core.Entities;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kota.Pokedex.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService {
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public AuthTokenResult GenerateToken(User user) {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
