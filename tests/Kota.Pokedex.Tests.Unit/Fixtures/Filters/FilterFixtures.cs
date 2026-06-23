using Kota.Pokedex.Core.Interfaces;

namespace Kota.Pokedex.Tests.Unit.Fixtures.Filters;

/// <summary>
/// Filter dropdown metadata fixtures.
/// </summary>
public static class FilterFixtures {
    public static IReadOnlyList<FilterOption> Types =>
    [
        new(0, "fire", "Fire"),
        new(0, "water", "Water"),
        new(0, "grass", "Grass"),
        new(0, "electric", "Electric")
    ];

    public static IReadOnlyList<FilterOption> Generations =>
    [
        new(1, "generation-i", "Generation I"),
        new(2, "generation-ii", "Generation II"),
        new(3, "generation-iii", "Generation III")
    ];

    public static IReadOnlyList<FilterOption> Abilities =>
        Enumerable.Range(1, 15)
            .Select(i => {
                var name = PokeApi.PokeApiFixtures.AbilityName(i);
                var display = char.ToUpperInvariant(name[0]) + name[1..];
                return new FilterOption(0, name, display);
            })
            .ToList();
}
