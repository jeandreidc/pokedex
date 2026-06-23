namespace Kota.Pokedex.Core.Models;

public class PokemonCardDetails {
    public List<string> Types { get; set; } = [];
    public List<string> Abilities { get; set; } = [];
    public string? Generation { get; set; }
}
