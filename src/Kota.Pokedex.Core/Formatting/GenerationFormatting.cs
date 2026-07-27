namespace Kota.Pokedex.Core.Formatting;

/// <summary>
/// Shared generation display formatting (P2.3) — cards and filters use the same labels.
/// </summary>
public static class GenerationFormatting {
    public static string ToDisplayName(string nameOrSlug) {
        var roman = nameOrSlug.Replace("generation-", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        return roman switch {
            "I" => "Generation I",
            "II" => "Generation II",
            "III" => "Generation III",
            "IV" => "Generation IV",
            "V" => "Generation V",
            "VI" => "Generation VI",
            "VII" => "Generation VII",
            "VIII" => "Generation VIII",
            "IX" => "Generation IX",
            _ => nameOrSlug
        };
    }
}
