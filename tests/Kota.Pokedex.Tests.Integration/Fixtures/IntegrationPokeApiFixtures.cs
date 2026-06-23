using Kota.Pokedex.Core.Models.PokeApi;

namespace Kota.Pokedex.Tests.Integration.Fixtures;

/// <summary>
/// Deterministic PokeAPI payloads for integration tests (25 Pokémon, 15 abilities).
/// </summary>
public static class IntegrationPokeApiFixtures {
    public const string BaseUrl = "https://pokeapi.co/api/v2/";
    public const int TotalPokemon = 25;
    public const int TotalAbilities = 15;

    public static PokeApiListResponse PokemonListPage(int offset, int limit) {
        var count = Math.Min(limit, TotalPokemon - offset);
        var results = Enumerable.Range(offset + 1, count)
            .Select(id => NamedResource(PokemonName(id), $"pokemon/{id}/"))
            .ToList();

        var nextOffset = offset + limit;
        return new PokeApiListResponse {
            Count = TotalPokemon,
            Next = nextOffset < TotalPokemon ? $"{BaseUrl}pokemon?offset={nextOffset}&limit={limit}" : null,
            Previous = offset > 0 ? $"{BaseUrl}pokemon?offset={Math.Max(0, offset - limit)}&limit={limit}" : null,
            Results = results
        };
    }

    public static PokeApiListResponse TypeList() => new() {
        Count = 4,
        Results = [
            NamedResource("fire", "type/10/"),
            NamedResource("water", "type/11/"),
            NamedResource("grass", "type/12/"),
            NamedResource("electric", "type/13/")
        ]
    };

    public static PokeApiListResponse AbilityList() => new() {
        Count = TotalAbilities,
        Results = Enumerable.Range(1, TotalAbilities)
            .Select(i => NamedResource(AbilityName(i), $"ability/{i}/"))
            .ToList()
    };

    public static PokeApiListResponse GenerationList() => new() {
        Count = 3,
        Results = [
            NamedResource("generation-i", "generation/1/"),
            NamedResource("generation-ii", "generation/2/"),
            NamedResource("generation-iii", "generation/3/")
        ]
    };

    public static PokeApiTypeDetail FireTypeDetail() => new() {
        Id = 10,
        Name = "fire",
        Pokemon = PokemonRefs(4, 5, 6).Select(p => new PokeApiTypePokemon { Pokemon = p }).ToList()
    };

    public static PokeApiAbilityDetail AbilityDetail(string name) => new() {
        Id = 1,
        Name = name,
        Pokemon = name switch {
            "overgrow" => PokemonRefs(1, 2, 3).Select(p => new PokeApiAbilityPokemon { Pokemon = p }).ToList(),
            "blaze" => PokemonRefs(4, 5, 6).Select(p => new PokeApiAbilityPokemon { Pokemon = p }).ToList(),
            _ => []
        }
    };

    public static PokeApiGenerationDetail GenerationOneDetail() => new() {
        Id = 1,
        Name = "generation-i",
        PokemonSpecies = Enumerable.Range(1, TotalPokemon)
            .Select(id => NamedResource(PokemonName(id), $"pokemon-species/{id}/"))
            .ToList()
    };

    public static PokeApiGenerationDetail GenerationTwoDetail() => new() {
        Id = 2,
        Name = "generation-ii",
        PokemonSpecies = []
    };

    public static PokeApiGenerationDetail GenerationThreeDetail() => new() {
        Id = 3,
        Name = "generation-iii",
        PokemonSpecies = []
    };

    public static PokeApiPokemonDetail PokemonDetail(int id) {
        var abilityName = id switch {
            >= 1 and <= 3 => "overgrow",
            >= 4 and <= 6 => "blaze",
            >= 7 and <= 9 => "torrent",
            25 => "static",
            _ => "keen-eye"
        };

        return new PokeApiPokemonDetail {
            Id = id,
            Name = PokemonName(id),
            Types = TypesFor(id).Select((t, index) => new PokeApiPokemonTypeSlot {
                Slot = index + 1,
                Type = new PokeApiNamedResource { Name = t, Url = $"{BaseUrl}type/{t}/" }
            }).ToList(),
            Abilities =
            [
                new PokeApiPokemonAbilitySlot {
                    Slot = 1,
                    IsHidden = false,
                    Ability = new PokeApiNamedResource {
                        Name = abilityName,
                        Url = $"{BaseUrl}ability/{abilityName}/"
                    }
                }
            ]
        };
    }

    public static string PokemonName(int id) => id switch {
        1 => "bulbasaur",
        2 => "ivysaur",
        3 => "venusaur",
        4 => "charmander",
        5 => "charmeleon",
        6 => "charizard",
        7 => "squirtle",
        8 => "wartortle",
        9 => "blastoise",
        25 => "pikachu",
        _ => $"pokemon-{id}"
    };

    public static string AbilityName(int id) => id switch {
        1 => "overgrow",
        2 => "blaze",
        3 => "torrent",
        4 => "static",
        5 => "intimidate",
        _ => $"ability-{id}"
    };

    public static IReadOnlyList<string> TypesFor(int id) => id switch {
        >= 4 and <= 6 => ["fire"],
        >= 7 and <= 9 => ["water"],
        >= 1 and <= 3 => ["grass", "poison"],
        25 => ["electric"],
        _ => ["normal"]
    };

    public static PokeApiNamedResource NamedResource(string name, string path) =>
        new() { Name = name, Url = $"{BaseUrl}{path}" };

    public static IEnumerable<PokeApiNamedResource> PokemonRefs(params int[] ids) =>
        ids.Select(id => NamedResource(PokemonName(id), $"pokemon/{id}/"));
}
