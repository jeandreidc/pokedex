namespace Kota.Pokedex.Core.Options;

public class CacheOptions {
    public const string SectionName = "Cache";

    public string Provider { get; set; } = "Memory";
    public int DefaultTtlMinutes { get; set; } = 1440;
}
