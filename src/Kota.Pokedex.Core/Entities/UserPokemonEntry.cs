namespace Kota.Pokedex.Core.Entities;

public class UserPokemonEntry {
    public Guid UserId { get; set; }
    public int PokemonId { get; set; }
    public bool IsCaught { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
