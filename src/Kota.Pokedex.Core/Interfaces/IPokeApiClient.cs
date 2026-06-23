using Kota.Pokedex.Core.Models.PokeApi;

namespace Kota.Pokedex.Core.Interfaces;

public interface IPokeApiClient {
    Task<PokeApiListResponse> GetPokemonListAsync(int limit, int offset, CancellationToken cancellationToken = default);
    Task<PokeApiTypeDetail> GetTypeAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<PokeApiAbilityDetail> GetAbilityAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<PokeApiGenerationDetail> GetGenerationAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<PokeApiPokemonDetail> GetPokemonAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<PokeApiListResponse> GetTypeListAsync(int limit, int offset, CancellationToken cancellationToken = default);
    Task<PokeApiListResponse> GetAbilityListAsync(int limit, int offset, CancellationToken cancellationToken = default);
    Task<PokeApiListResponse> GetGenerationListAsync(int limit, int offset, CancellationToken cancellationToken = default);
}
