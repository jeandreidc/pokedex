using Kota.Pokedex.Core.Entities;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kota.Pokedex.Infrastructure.Repositories;

public class UserRepository : IUserRepository {
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
