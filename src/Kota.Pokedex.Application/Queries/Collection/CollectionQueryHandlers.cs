using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Interfaces;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Collection;

public class GetCollectionQueryHandler : IRequestHandler<GetCollectionQuery, IReadOnlyList<CollectionEntryDto>> {
    private readonly ICollectionRepository _collectionRepository;
    private readonly IPokemonIndexService _indexService;
    private readonly ICurrentUserService _currentUser;

    public GetCollectionQueryHandler(
        ICollectionRepository collectionRepository,
        IPokemonIndexService indexService,
        ICurrentUserService currentUser) {
        _collectionRepository = collectionRepository;
        _indexService = indexService;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CollectionEntryDto>> Handle(GetCollectionQuery request, CancellationToken cancellationToken) {
        var userId = RequireUserId();
        var entries = await _collectionRepository.GetEntriesAsync(
            userId,
            request.FavoritesOnly,
            request.CaughtOnly,
            cancellationToken);

        var results = new List<CollectionEntryDto>();
        foreach (var entry in entries) {
            var pokemon = await _indexService.GetEntryAsync(entry.PokemonId, cancellationToken);
            if (pokemon is null) continue;

            results.Add(new CollectionEntryDto(
                pokemon.Id,
                pokemon.Name,
                pokemon.SpriteUrl,
                entry.IsCaught,
                entry.IsFavorite));
        }

        return results;
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
}

public class GetCollectionEntryQueryHandler : IRequestHandler<GetCollectionEntryQuery, CollectionEntryDto?> {
    private readonly ICollectionRepository _collectionRepository;
    private readonly IPokemonIndexService _indexService;
    private readonly ICurrentUserService _currentUser;

    public GetCollectionEntryQueryHandler(
        ICollectionRepository collectionRepository,
        IPokemonIndexService indexService,
        ICurrentUserService currentUser) {
        _collectionRepository = collectionRepository;
        _indexService = indexService;
        _currentUser = currentUser;
    }

    public async Task<CollectionEntryDto?> Handle(GetCollectionEntryQuery request, CancellationToken cancellationToken) {
        var userId = RequireUserId();
        var entry = await _collectionRepository.GetEntryAsync(userId, request.PokemonId, cancellationToken);
        if (entry is null) return null;

        var pokemon = await _indexService.GetEntryAsync(request.PokemonId, cancellationToken);
        if (pokemon is null) return null;

        return new CollectionEntryDto(
            pokemon.Id,
            pokemon.Name,
            pokemon.SpriteUrl,
            entry.IsCaught,
            entry.IsFavorite);
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
}

public class GetCollectionStatsQueryHandler : IRequestHandler<GetCollectionStatsQuery, CollectionStatsDto> {
    private readonly ICollectionRepository _collectionRepository;
    private readonly IPokemonIndexService _indexService;
    private readonly IFilterMetadataService _filterMetadataService;
    private readonly ICurrentUserService _currentUser;

    public GetCollectionStatsQueryHandler(
        ICollectionRepository collectionRepository,
        IPokemonIndexService indexService,
        IFilterMetadataService filterMetadataService,
        ICurrentUserService currentUser) {
        _collectionRepository = collectionRepository;
        _indexService = indexService;
        _filterMetadataService = filterMetadataService;
        _currentUser = currentUser;
    }

    public async Task<CollectionStatsDto> Handle(GetCollectionStatsQuery request, CancellationToken cancellationToken) {
        var userId = RequireUserId();

        var allEntries = await _collectionRepository.GetEntriesAsync(userId, cancellationToken: cancellationToken);
        var caughtIds = allEntries.Where(e => e.IsCaught).Select(e => e.PokemonId).ToHashSet();
        var favoriteCount = allEntries.Count(e => e.IsFavorite);

        var index = await _indexService.GetIndexAsync(cancellationToken);
        var totalPokemon = index.Count;
        var overallPercentage = totalPokemon == 0 ? 0 : Math.Round(caughtIds.Count * 100.0 / totalPokemon, 1);

        var generations = await _filterMetadataService.GetGenerationsAsync(cancellationToken);
        var byGeneration = new List<GenerationStatDto>();

        foreach (var generation in generations) {
            var generationIds = await _indexService.GetPokemonIdsByGenerationAsync(generation.Name, cancellationToken);
            var totalInGeneration = generationIds.Count;
            var caughtInGeneration = generationIds.Count(id => caughtIds.Contains(id));
            var percentage = totalInGeneration == 0
                ? 0
                : Math.Round(caughtInGeneration * 100.0 / totalInGeneration, 1);

            byGeneration.Add(new GenerationStatDto(
                generation.Name,
                generation.DisplayName,
                caughtInGeneration,
                totalInGeneration,
                percentage));
        }

        return new CollectionStatsDto(
            caughtIds.Count,
            favoriteCount,
            totalPokemon,
            overallPercentage,
            byGeneration);
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
