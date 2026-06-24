using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models;
using Kota.Pokedex.Tests.Unit.Fixtures.Index;

namespace Kota.Pokedex.Tests.Unit.Helpers.Mocks;

public static class PokemonIndexServiceMock {
    public static Mock<IPokemonIndexService> CreateDefault() {
        var mock = new Mock<IPokemonIndexService>();

        mock.Setup(s => s.GetIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokemonIndexFixtures.AllEntries);

        mock.Setup(s => s.GetPokemonIdsByTypeAsync("fire", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokemonIndexFixtures.FireTypeIds);

        mock.Setup(s => s.GetPokemonIdsByTypeAsync("water", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokemonIndexFixtures.WaterTypeIds);

        mock.Setup(s => s.GetPokemonIdsByAbilityAsync("overgrow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokemonIndexFixtures.OvergrowAbilityIds);

        mock.Setup(s => s.GetPokemonIdsByGenerationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokemonIndexFixtures.GenerationOneIds);

        mock.Setup(s => s.GetEntryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => PokemonIndexFixtures.CreateEntry(id));

        mock.Setup(s => s.GetPokemonCardDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => CardDetailsFor(id));

        mock.Setup(s => s.GetCachedCardDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => (PokemonCardDetails?)null);

        return mock;
    }

    private static PokemonCardDetails CardDetailsFor(int id) => new() {
        Types = TypesFor(id).ToList(),
        Abilities = [AbilityFor(id)],
        Generation = id <= 151 ? "I" : "II"
    };

    private static IReadOnlyList<string> TypesFor(int id) => id switch {
        >= 4 and <= 6 => ["fire"],
        >= 7 and <= 9 => ["water"],
        >= 1 and <= 3 => ["grass", "poison"],
        25 => ["electric"],
        _ => ["normal"]
    };

    private static string AbilityFor(int id) => id switch {
        >= 1 and <= 3 => "Overgrow",
        >= 4 and <= 6 => "Blaze",
        >= 7 and <= 9 => "Torrent",
        25 => "Static",
        _ => "Keen Eye"
    };
}

public static class FilterMetadataServiceMock {
    public static Mock<IFilterMetadataService> CreateDefault() {
        var mock = new Mock<IFilterMetadataService>();

        mock.Setup(s => s.GetTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Fixtures.Filters.FilterFixtures.Types);

        mock.Setup(s => s.GetGenerationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Fixtures.Filters.FilterFixtures.Generations);

        mock.Setup(s => s.GetAbilitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Fixtures.Filters.FilterFixtures.Abilities);

        return mock;
    }
}
