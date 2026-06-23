namespace Kota.Pokedex.Core.Entities;

public class User {
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<UserPokemonEntry> CollectionEntries { get; set; } = [];
}
