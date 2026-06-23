namespace Kota.Pokedex.Core.Constants;

public static class CacheKeys {
    public const string PokemonIndex = "pokeapi:pokemon-index";
    public const string FilterTypes = "pokeapi:filters:types";
    public const string FilterAbilities = "pokeapi:filters:abilities";
    public const string FilterGenerations = "pokeapi:filters:generations";

    public static string Type(string name) => $"pokeapi:type:{name.ToLowerInvariant()}";
    public static string Ability(string name) => $"pokeapi:ability:{name.ToLowerInvariant()}";
    public static string Generation(string name) => $"pokeapi:generation:{name.ToLowerInvariant()}";
    public static string PokemonDetail(int id) => $"pokeapi:pokemon:{id}";
}
