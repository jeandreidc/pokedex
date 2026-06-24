using Kota.Pokedex.Core.Models;

namespace Kota.Pokedex.Core.Interfaces;

public interface IPokemonIndexService {
    Task<IReadOnlyList<PokemonIndexEntry>> GetIndexAsync(CancellationToken cancellationToken = default);
    Task WarmupAsync(CancellationToken cancellationToken = default);
    Task PrefetchFirstPageCardDetailsAsync(int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<int>> GetPokemonIdsByTypeAsync(string type, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<int>> GetPokemonIdsByAbilityAsync(string ability, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<int>> GetPokemonIdsByGenerationAsync(string generation, CancellationToken cancellationToken = default);
    Task<PokemonIndexEntry?> GetEntryAsync(int id, CancellationToken cancellationToken = default);
    Task<PokemonCardDetails> GetPokemonCardDetailsAsync(int id, CancellationToken cancellationToken = default);
}
