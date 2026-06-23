using Kota.Pokedex.Core.Entities;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kota.Pokedex.Infrastructure.Repositories;

public class CollectionRepository : ICollectionRepository {
    private readonly AppDbContext _context;

    public CollectionRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<UserPokemonEntry>> GetEntriesAsync(
        Guid userId,
        bool? favoritesOnly = null,
        bool? caughtOnly = null,
        CancellationToken cancellationToken = default) {
        var query = _context.UserPokemonEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        if (favoritesOnly == true) {
            query = query.Where(e => e.IsFavorite);
        }

        if (caughtOnly == true) {
            query = query.Where(e => e.IsCaught);
        }

        return await query.OrderBy(e => e.PokemonId).ToListAsync(cancellationToken);
    }

    public Task<UserPokemonEntry?> GetEntryAsync(Guid userId, int pokemonId, CancellationToken cancellationToken = default) =>
        _context.UserPokemonEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.PokemonId == pokemonId, cancellationToken);

    public async Task UpsertAsync(UserPokemonEntry entry, CancellationToken cancellationToken = default) {
        var existing = await _context.UserPokemonEntries
            .FirstOrDefaultAsync(e => e.UserId == entry.UserId && e.PokemonId == entry.PokemonId, cancellationToken);

        if (existing is null) {
            await _context.UserPokemonEntries.AddAsync(entry, cancellationToken);
            return;
        }

        existing.IsCaught = entry.IsCaught;
        existing.IsFavorite = entry.IsFavorite;
        existing.UpdatedAtUtc = entry.UpdatedAtUtc;
    }

    public async Task RemoveAsync(Guid userId, int pokemonId, CancellationToken cancellationToken = default) {
        var existing = await _context.UserPokemonEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.PokemonId == pokemonId, cancellationToken);

        if (existing is not null) {
            _context.UserPokemonEntries.Remove(existing);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
