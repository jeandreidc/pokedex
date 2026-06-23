using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models.PokeApi;
using Kota.Pokedex.Tests.Integration.Fixtures;

namespace Kota.Pokedex.Tests.Integration.Support;

public sealed class FakePokeApiClient : IPokeApiClient {
    public Task<PokeApiListResponse> GetPokemonListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationPokeApiFixtures.PokemonListPage(offset, limit));

    public Task<PokeApiListResponse> GetTypeListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationPokeApiFixtures.TypeList());

    public Task<PokeApiListResponse> GetAbilityListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationPokeApiFixtures.AbilityList());

    public Task<PokeApiListResponse> GetGenerationListAsync(int limit, int offset, CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationPokeApiFixtures.GenerationList());

    public Task<PokeApiTypeDetail> GetTypeAsync(string nameOrId, CancellationToken cancellationToken = default) =>
        nameOrId.Equals("fire", StringComparison.OrdinalIgnoreCase)
            ? Task.FromResult(IntegrationPokeApiFixtures.FireTypeDetail())
            : throw new InvalidOperationException($"Unknown type: {nameOrId}");

    public Task<PokeApiAbilityDetail> GetAbilityAsync(string nameOrId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationPokeApiFixtures.AbilityDetail(nameOrId));

    public Task<PokeApiGenerationDetail> GetGenerationAsync(string nameOrId, CancellationToken cancellationToken = default) {
        var normalized = nameOrId.ToLowerInvariant() switch {
            "1" or "generation-i" => "generation-i",
            "2" or "generation-ii" => "generation-ii",
            "3" or "generation-iii" => "generation-iii",
            _ => nameOrId.ToLowerInvariant()
        };

        return Task.FromResult(normalized switch {
            "generation-i" => IntegrationPokeApiFixtures.GenerationOneDetail(),
            "generation-ii" => IntegrationPokeApiFixtures.GenerationTwoDetail(),
            "generation-iii" => IntegrationPokeApiFixtures.GenerationThreeDetail(),
            _ => throw new InvalidOperationException($"Unknown generation: {nameOrId}")
        });
    }

    public Task<PokeApiPokemonDetail> GetPokemonAsync(string nameOrId, CancellationToken cancellationToken = default) {
        if (int.TryParse(nameOrId, out var id) && id is >= 1 and <= IntegrationPokeApiFixtures.TotalPokemon) {
            return Task.FromResult(IntegrationPokeApiFixtures.PokemonDetail(id));
        }

        for (var pokemonId = 1; pokemonId <= IntegrationPokeApiFixtures.TotalPokemon; pokemonId++) {
            if (IntegrationPokeApiFixtures.PokemonName(pokemonId).Equals(nameOrId, StringComparison.OrdinalIgnoreCase)) {
                return Task.FromResult(IntegrationPokeApiFixtures.PokemonDetail(pokemonId));
            }
        }

        throw new InvalidOperationException($"Unknown pokemon: {nameOrId}");
    }
}
