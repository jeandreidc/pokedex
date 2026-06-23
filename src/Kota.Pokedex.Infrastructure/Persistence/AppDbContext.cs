using Kota.Pokedex.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kota.Pokedex.Infrastructure.Persistence;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPokemonEntry> UserPokemonEntries => Set<UserPokemonEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(entity => {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<UserPokemonEntry>(entity => {
            entity.HasKey(e => new { e.UserId, e.PokemonId });
            entity.HasOne(e => e.User)
                .WithMany(u => u.CollectionEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
