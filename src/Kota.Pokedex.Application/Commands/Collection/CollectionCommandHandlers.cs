using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Interfaces;
using Kota.Pokedex.Core.Entities;
using Kota.Pokedex.Core.Interfaces;
using MediatR;

namespace Kota.Pokedex.Application.Commands.Collection;

public class UpdateCollectionEntryCommandHandler : IRequestHandler<UpdateCollectionEntryCommand, CollectionEntryDto> {
    private readonly ICollectionRepository _collectionRepository;
    private readonly IPokemonIndexService _indexService;
    private readonly ICurrentUserService _currentUser;

    public UpdateCollectionEntryCommandHandler(
        ICollectionRepository collectionRepository,
        IPokemonIndexService indexService,
        ICurrentUserService currentUser) {
        _collectionRepository = collectionRepository;
        _indexService = indexService;
        _currentUser = currentUser;
    }

    public async Task<CollectionEntryDto> Handle(UpdateCollectionEntryCommand request, CancellationToken cancellationToken) {
        var userId = RequireUserId();

        var pokemon = await _indexService.GetEntryAsync(request.PokemonId, cancellationToken);
        if (pokemon is null) {
            throw new KeyNotFoundException($"Pokemon with id {request.PokemonId} was not found.");
        }

        var existing = await _collectionRepository.GetEntryAsync(userId, request.PokemonId, cancellationToken);
        var isCaught = request.IsCaught ?? existing?.IsCaught ?? false;
        var isFavorite = request.IsFavorite ?? existing?.IsFavorite ?? false;

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

        return new CollectionEntryDto(pokemon.Id, pokemon.Name, pokemon.SpriteUrl, isCaught, isFavorite);
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
