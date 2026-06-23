using System.Text.Json.Serialization;

namespace Kota.Pokedex.Core.Models.PokeApi;

public class PokeApiListResponse {
    public int Count { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public List<PokeApiNamedResource> Results { get; set; } = [];
}

public class PokeApiNamedResource {
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class PokeApiTypeDetail {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PokeApiTypePokemon> Pokemon { get; set; } = [];
}

public class PokeApiTypePokemon {
    public PokeApiNamedResource Pokemon { get; set; } = new();
}

public class PokeApiAbilityDetail {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PokeApiAbilityPokemon> Pokemon { get; set; } = [];
}

public class PokeApiAbilityPokemon {
    public PokeApiNamedResource Pokemon { get; set; } = new();
}

public class PokeApiGenerationDetail {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pokemon_species")]
    public List<PokeApiNamedResource> PokemonSpecies { get; set; } = [];
}

public class PokeApiPokemonDetail {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PokeApiPokemonAbilitySlot> Abilities { get; set; } = [];
    public List<PokeApiPokemonTypeSlot> Types { get; set; } = [];
    public PokeApiPokemonSprites Sprites { get; set; } = new();
}

public class PokeApiPokemonAbilitySlot {
    public bool IsHidden { get; set; }
    public int Slot { get; set; }
    public PokeApiNamedResource Ability { get; set; } = new();
}

public class PokeApiPokemonTypeSlot {
    public int Slot { get; set; }
    public PokeApiNamedResource Type { get; set; } = new();
}

public class PokeApiPokemonSprites {
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }

    public PokeApiPokemonOtherSprites? Other { get; set; }
}

public class PokeApiPokemonOtherSprites {
    [JsonPropertyName("official-artwork")]
    public PokeApiOfficialArtwork? OfficialArtwork { get; set; }
}

public class PokeApiOfficialArtwork {
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }
}
