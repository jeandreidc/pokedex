namespace Kota.Pokedex.Application.DTOs;

public record AuthResponseDto(string Token, string Username, DateTime ExpiresAtUtc);

public record CollectionEntryDto(
    int PokemonId,
    string Name,
    string SpriteUrl,
    bool IsCaught,
    bool IsFavorite);

public record UpdateCollectionEntryRequest(bool? IsCaught, bool? IsFavorite);

public record CollectionStatsDto(
    int TotalCaught,
    int TotalFavorites,
    int TotalPokemon,
    double OverallCaughtPercentage,
    IReadOnlyList<GenerationStatDto> ByGeneration);

public record GenerationStatDto(
    string Generation,
    string DisplayName,
    int CaughtCount,
    int TotalInGeneration,
    double CaughtPercentage);
