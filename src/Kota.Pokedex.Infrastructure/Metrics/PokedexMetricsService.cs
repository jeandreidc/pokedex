using System.Diagnostics.Metrics;
using Kota.Pokedex.Application.Interfaces;

namespace Kota.Pokedex.Infrastructure.Metrics;

public sealed class PokedexMetricsService : IPokedexMetricsService {
    public const string MeterName = "Kota.Pokedex";

    private readonly Counter<long> _pokemonFavorited;
    private readonly Counter<long> _pokemonCaught;
    private readonly Counter<long> _usersRegistered;
    private readonly Counter<long> _usersLoggedIn;

    public PokedexMetricsService() {
        var meter = new Meter(MeterName, "1.0.0");
        _pokemonFavorited = meter.CreateCounter<long>(
            "pokedex.pokemon.favorited",
            unit: "count",
            description: "Pokemon marked as favorite");
        _pokemonCaught = meter.CreateCounter<long>(
            "pokedex.pokemon.caught",
            unit: "count",
            description: "Pokemon marked as caught");
        _usersRegistered = meter.CreateCounter<long>(
            "pokedex.users.registered",
            unit: "count",
            description: "Users who completed registration");
        _usersLoggedIn = meter.CreateCounter<long>(
            "pokedex.users.logged_in",
            unit: "count",
            description: "Successful user logins");
    }

    public void RecordPokemonFavorited(string pokemonName, string generation) {
        _pokemonFavorited.Add(1,
            new KeyValuePair<string, object?>("pokemon_name", pokemonName),
            new KeyValuePair<string, object?>("generation", generation));
    }

    public void RecordPokemonCaught(string pokemonName, string generation) {
        _pokemonCaught.Add(1,
            new KeyValuePair<string, object?>("pokemon_name", pokemonName),
            new KeyValuePair<string, object?>("generation", generation));
    }

    public void RecordUserRegistered() => _usersRegistered.Add(1);

    public void RecordUserLoggedIn() => _usersLoggedIn.Add(1);
}
