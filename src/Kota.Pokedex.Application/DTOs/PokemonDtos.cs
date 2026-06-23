namespace Kota.Pokedex.Application.DTOs;

public class PokemonSummaryDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpriteUrl { get; set; } = string.Empty;
    public List<string> Types { get; set; } = [];
    public List<string> Abilities { get; set; } = [];
    public string? Generation { get; set; }
}

public class FilterOptionDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
