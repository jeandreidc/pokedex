namespace Kota.Pokedex.Core.Options;

public class PokeApiOptions {
    public const string SectionName = "PokeApi";

    public string BaseUrl { get; set; } = "https://pokeapi.co/api/v2/";
    public int MaxConcurrentRequests { get; set; } = 5;
    public int PageFetchLimit { get; set; } = 100;
}
