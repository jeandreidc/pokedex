using Kota.Pokedex.Core.Models;

namespace Kota.Pokedex.Tests.Unit.Fixtures.Index;

/// <summary>
/// In-memory pokemon index entries mirroring prefetched data (25 records for pagination tests).
/// </summary>
public static class PokemonIndexFixtures {
    public const int TotalCount = 25;

    public static IReadOnlyList<PokemonIndexEntry> AllEntries =>
        Enumerable.Range(1, TotalCount).Select(CreateEntry).ToList();

    public static PokemonIndexEntry CreateEntry(int id) => new() {
        Id = id,
        Name = PokeApi.PokeApiFixtures.PokemonName(id),
        SpriteUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png"
    };

    public static IReadOnlySet<int> FireTypeIds => new HashSet<int> { 4, 5, 6 };
    public static IReadOnlySet<int> WaterTypeIds => new HashSet<int> { 7, 8, 9 };
    public static IReadOnlySet<int> OvergrowAbilityIds => new HashSet<int> { 1, 2, 3 };
    public static IReadOnlySet<int> GenerationOneIds => Enumerable.Range(1, TotalCount).ToHashSet();
}
