using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Interfaces;
using Kota.Pokedex.Application.Notifications;
using Kota.Pokedex.Core.Entities;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Commands.Collection;

public class UpdateCollectionEntryCommandHandler : IRequestHandler<UpdateCollectionEntryCommand, CollectionEntryDto> {
    private readonly ICollectionRepository _collectionRepository;
    private readonly IPokemonIndexService _indexService;
    private readonly ICurrentUserService _currentUser;
    private readonly IPublisher _publisher;

    public UpdateCollectionEntryCommandHandler(
        ICollectionRepository collectionRepository,
        IPokemonIndexService indexService,
        ICurrentUserService currentUser,
        IPublisher publisher) {
        _collectionRepository = collectionRepository;
        _indexService = indexService;
        _currentUser = currentUser;
        _publisher = publisher;
    }

    public async Task<CollectionEntryDto> Handle(UpdateCollectionEntryCommand request, CancellationToken cancellationToken) {
        var userId = RequireUserId();

        var pokemon = await _indexService.GetEntryAsync(request.PokemonId, cancellationToken);
        if (pokemon is null) {
            throw new KeyNotFoundException($"Pokemon with id {request.PokemonId} was not found.");
        }

        var existing = await _collectionRepository.GetEntryAsync(userId, request.PokemonId, cancellationToken);
        var wasFavorite = existing?.IsFavorite ?? false;
        var wasCaught = existing?.IsCaught ?? false;
        var isCaught = request.IsCaught ?? wasCaught;
        var isFavorite = request.IsFavorite ?? wasFavorite;

        if (!isCaught && !isFavorite) {
            await _collectionRepository.RemoveAsync(userId, request.PokemonId, cancellationToken);
            await _collectionRepository.SaveChangesAsync(cancellationToken);
            return new CollectionEntryDto(pokemon.Id, pokemon.Name, pokemon.SpriteUrl, false, false);
        }

        var entry = new UserPokemonEntry {
            UserId = userId,
            PokemonId = request.PokemonId,
            IsCaught = isCaught,
            IsFavorite = isFavorite,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _collectionRepository.UpsertAsync(entry, cancellationToken);
        await _collectionRepository.SaveChangesAsync(cancellationToken);

        await PublishCollectionMetricsIfNeeded(
            pokemon.Id,
            pokemon.Name,
            wasFavorite,
            wasCaught,
            isFavorite,
            isCaught,
            cancellationToken);

        return new CollectionEntryDto(pokemon.Id, pokemon.Name, pokemon.SpriteUrl, isCaught, isFavorite);
    }

    private async Task PublishCollectionMetricsIfNeeded(
        int pokemonId,
        string pokemonName,
        bool wasFavorite,
        bool wasCaught,
        bool isFavorite,
        bool isCaught,
        CancellationToken cancellationToken) {
        var markedFavorite = isFavorite && !wasFavorite;
        var markedCaught = isCaught && !wasCaught;
        if (!markedFavorite && !markedCaught) {
            return;
        }

        var cardDetails = await _indexService.GetPokemonCardDetailsAsync(pokemonId, cancellationToken);
        var generation = cardDetails.Generation ?? "unknown";

        if (markedFavorite) {
            await _publisher.Publish(
                new PokemonMarkedFavoriteNotification(pokemonId, pokemonName, generation),
                cancellationToken);
        }

        if (markedCaught) {
            await _publisher.Publish(
                new PokemonMarkedCaughtNotification(pokemonId, pokemonName, generation),
                cancellationToken);
        }
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
}

public class RemoveCollectionEntryCommandHandler : IRequestHandler<RemoveCollectionEntryCommand, Unit> {
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICurrentUserService _currentUser;

    public RemoveCollectionEntryCommandHandler(
        ICollectionRepository collectionRepository,
        ICurrentUserService currentUser) {
        _collectionRepository = collectionRepository;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RemoveCollectionEntryCommand request, CancellationToken cancellationToken) {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _collectionRepository.RemoveAsync(userId, request.PokemonId, cancellationToken);
        await _collectionRepository.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
