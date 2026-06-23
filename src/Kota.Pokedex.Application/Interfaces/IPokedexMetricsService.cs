namespace Kota.Pokedex.Application.Interfaces;

public interface IPokedexMetricsService {
    void RecordPokemonFavorited(string pokemonName, string generation);
    void RecordPokemonCaught(string pokemonName, string generation);
    void RecordUserRegistered();
    void RecordUserLoggedIn();
}
