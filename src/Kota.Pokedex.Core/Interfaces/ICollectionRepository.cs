using Kota.Pokedex.Core.Entities;

namespace Kota.Pokedex.Core.Interfaces;

public interface ICollectionRepository {
    Task<IReadOnlyList<UserPokemonEntry>> GetEntriesAsync(
        Guid userId,
        bool? favoritesOnly = null,
        bool? caughtOnly = null,
        CancellationToken cancellationToken = default);

    Task<UserPokemonEntry?> GetEntryAsync(Guid userId, int pokemonId, CancellationToken cancellationToken = default);

    Task UpsertAsync(UserPokemonEntry entry, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid userId, int pokemonId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
