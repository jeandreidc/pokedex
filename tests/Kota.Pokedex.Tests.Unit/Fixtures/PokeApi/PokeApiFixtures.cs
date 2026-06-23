using Kota.Pokedex.Core.Models;
using Kota.Pokedex.Core.Models.PokeApi;

namespace Kota.Pokedex.Tests.Unit.Fixtures.PokeApi;

/// <summary>
/// Small PokeAPI-shaped payloads for unit tests (subset of real API structure).
/// </summary>
public static class PokeApiFixtures {
    public const string BaseUrl = "https://pokeapi.co/api/v2/";

    public static PokeApiListResponse PokemonListPage(int offset, int pageSize, int totalCount) {
        var results = Enumerable.Range(offset + 1, Math.Min(pageSize, totalCount - offset))
            .Select(id => NamedResource(PokemonName(id), $"pokemon/{id}/"))
            .ToList();

        var nextOffset = offset + pageSize;
        return new PokeApiListResponse {
            Count = totalCount,
            Next = nextOffset < totalCount ? $"{BaseUrl}pokemon?offset={nextOffset}&limit={pageSize}" : null,
            Previous = offset > 0 ? $"{BaseUrl}pokemon?offset={Math.Max(0, offset - pageSize)}&limit={pageSize}" : null,
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
        Count = 15,
        Results = Enumerable.Range(1, 15)
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

    public static PokeApiTypeDetail WaterTypeDetail() => new() {
        Id = 11,
        Name = "water",
        Pokemon = PokemonRefs(7, 8, 9).Select(p => new PokeApiTypePokemon { Pokemon = p }).ToList()
    };

    public static PokeApiAbilityDetail OvergrowAbility() => new() {
        Id = 1,
        Name = "overgrow",
        Pokemon = PokemonRefs(1, 2, 3).Select(p => new PokeApiAbilityPokemon { Pokemon = p }).ToList()
    };

    public static PokeApiAbilityDetail BlazeAbility() => new() {
        Id = 2,
        Name = "blaze",
        Pokemon = PokemonRefs(4, 5, 6).Select(p => new PokeApiAbilityPokemon { Pokemon = p }).ToList()
    };

    public static PokeApiGenerationDetail GenerationOneDetail() => new() {
        Id = 1,
        Name = "generation-i",
        PokemonSpecies = Enumerable.Range(1, 25)
            .Select(id => NamedResource(PokemonName(id), $"pokemon-species/{id}/"))
            .ToList()
    };

    public static PokeApiGenerationDetail GenerationTwoDetail() => new() {
        Id = 2,
        Name = "generation-ii",
        PokemonSpecies = Enumerable.Range(152, 5)
            .Select(id => NamedResource($"mon-{id}", $"pokemon-species/{id}/"))
            .ToList()
    };

    public static PokeApiPokemonDetail PokemonDetail(int id, params string[] types) => new() {
        Id = id,
        Name = PokemonName(id),
        Types = types.Select(t => new PokeApiPokemonTypeSlot {
            Type = new PokeApiNamedResource { Name = t, Url = $"{BaseUrl}type/{t}/" }
        }).ToList()
    };

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

    public static PokeApiNamedResource NamedResource(string name, string path) =>
        new() { Name = name, Url = $"{BaseUrl}{path}" };

    public static IEnumerable<PokeApiNamedResource> PokemonRefs(params int[] ids) =>
        ids.Select(id => NamedResource(PokemonName(id), $"pokemon/{id}/"));
}
