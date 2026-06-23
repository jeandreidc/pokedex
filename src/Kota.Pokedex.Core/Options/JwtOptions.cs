namespace Kota.Pokedex.Core.Options;

public class JwtOptions {
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Kota.Pokedex";
    public string Audience { get; set; } = "Kota.Pokedex.Client";
    public int ExpiryMinutes { get; set; } = 60;
}
