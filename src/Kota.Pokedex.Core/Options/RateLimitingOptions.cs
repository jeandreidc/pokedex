namespace Kota.Pokedex.Core.Options;

public class RateLimitingOptions {
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 100;
    public int WindowMinutes { get; set; } = 1;
}
