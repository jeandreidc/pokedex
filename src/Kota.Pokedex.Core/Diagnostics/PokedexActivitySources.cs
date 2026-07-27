using System.Diagnostics;

namespace Kota.Pokedex.Core.Diagnostics;

/// <summary>
/// Custom ActivitySource for business spans (P1.7). Name aligns with the Kota.Pokedex meter.
/// </summary>
public static class PokedexActivitySources {
    public const string Name = "Kota.Pokedex";

    public static readonly ActivitySource Source = new(Name);
}
