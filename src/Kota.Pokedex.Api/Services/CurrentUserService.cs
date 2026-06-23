using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kota.Pokedex.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Kota.Pokedex.Api.Services;

public class CurrentUserService : ICurrentUserService {
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId {
        get {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null) return null;

            var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Username =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
