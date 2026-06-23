using System.Net;
using Kota.Pokedex.Core.Exceptions;
using Kota.Pokedex.Core.Models.PokeApi;
using Kota.Pokedex.Infrastructure.ExternalServices.PokeApi;
using Kota.Pokedex.Tests.Unit.Fixtures.PokeApi;
using Kota.Pokedex.Tests.Unit.Helpers.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Kota.Pokedex.Tests.Unit.Infrastructure.ExternalServices;

public class PokeApiClientTests {
    private readonly MockHttpMessageHandler _handler;
    private readonly PokeApiClient _sut;

    public PokeApiClientTests() {
        _handler = new MockHttpMessageHandler(HandleRequest);
        var client = new HttpClient(_handler) { BaseAddress = new Uri(PokeApiFixtures.BaseUrl) };
        _sut = new PokeApiClient(client, TestOptions.PokeApi(), NullLogger<PokeApiClient>.Instance);
    }

    [Fact]
    public async Task GetPokemonListAsync_DeserializesListResponse() {
        var result = await _sut.GetPokemonListAsync(10, 0);

        result.Results.Should().HaveCount(10);
        result.Results[0].Name.Should().Be("bulbasaur");
        result.Next.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTypeAsync_DeserializesTypeDetail() {
        var result = await _sut.GetTypeAsync("fire");

        result.Name.Should().Be("fire");
        result.Pokemon.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAbilityAsync_DeserializesAbilityDetail() {
        var result = await _sut.GetAbilityAsync("overgrow");

        result.Name.Should().Be("overgrow");
        result.Pokemon.Select(p => p.Pokemon.Name).Should().Contain("bulbasaur");
    }

    [Fact]
    public async Task GetGenerationAsync_NormalizesNumericId() {
        var result = await _sut.GetGenerationAsync("1");

        result.Name.Should().Be("generation-i");
        result.PokemonSpecies.Should().HaveCount(25);
    }

    [Fact]
    public async Task GetGenerationAsync_AcceptsSlug() {
        var result = await _sut.GetGenerationAsync("generation-ii");

        result.Name.Should().Be("generation-ii");
    }

    [Fact]
    public async Task GetPokemonAsync_DeserializesPokemonDetail() {
        var result = await _sut.GetPokemonAsync("25");

        result.Name.Should().Be("pikachu");
        result.Types.Select(t => t.Type.Name).Should().Contain("electric");
    }

    [Fact]
    public async Task GetAsync_ThrowsPokeApiException_OnFailure() {
        var failingHandler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.NotFound());
        var client = new HttpClient(failingHandler) { BaseAddress = new Uri(PokeApiFixtures.BaseUrl) };
        var sut = new PokeApiClient(client, TestOptions.PokeApi(), NullLogger<PokeApiClient>.Instance);

        var act = () => sut.GetPokemonAsync("missing");

        await act.Should().ThrowAsync<PokeApiException>()
            .Where(ex => ex.StatusCode == (int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAsync_ThrowsPokeApiException_WhenResponseBodyIsEmpty() {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri(PokeApiFixtures.BaseUrl) };
        var sut = new PokeApiClient(client, TestOptions.PokeApi(), NullLogger<PokeApiClient>.Instance);

        var act = () => sut.GetPokemonAsync("25");

        await act.Should().ThrowAsync<PokeApiException>()
            .WithMessage("*empty response*");
    }

    [Fact]
    public async Task GetGenerationAsync_PassesThroughUnknownNumericId() {
        var handler = new MockHttpMessageHandler(request => {
            request.RequestUri!.AbsolutePath.Should().EndWith("/generation/99");
            return MockHttpMessageHandler.JsonResponse(new PokeApiGenerationDetail {
                Id = 99,
                Name = "generation-99",
                PokemonSpecies = []
            });
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri(PokeApiFixtures.BaseUrl) };
        var sut = new PokeApiClient(client, TestOptions.PokeApi(), NullLogger<PokeApiClient>.Instance);

        var result = await sut.GetGenerationAsync("99");

        result.Name.Should().Be("generation-99");
    }

    [Fact]
    public async Task GetTypeListAsync_ReturnsTypes() {
        var result = await _sut.GetTypeListAsync(100, 0);

        result.Results.Should().HaveCount(4);
        result.Results.Select(r => r.Name).Should().Contain("fire");
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request) {
        var path = request.RequestUri!.AbsolutePath.TrimStart('/');
        if (path.StartsWith("api/v2/", StringComparison.Ordinal)) {
            path = path["api/v2/".Length..];
        }

        var query = request.RequestUri.Query;
        if (query.Length > 0) {
            path += query;
        }

        if (path.StartsWith("pokemon?", StringComparison.Ordinal)) {
            return MockHttpMessageHandler.JsonResponse(
                PokeApiFixtures.PokemonListPage(ParseOffset(path), 10, 25));
        }

        if (path.StartsWith("type?", StringComparison.Ordinal)) {
            return MockHttpMessageHandler.JsonResponse(PokeApiFixtures.TypeList());
        }

        if (path.StartsWith("ability?", StringComparison.Ordinal)) {
            return MockHttpMessageHandler.JsonResponse(PokeApiFixtures.AbilityList());
        }

        if (path.StartsWith("generation?", StringComparison.Ordinal)) {
            return MockHttpMessageHandler.JsonResponse(PokeApiFixtures.GenerationList());
        }

        return path switch {
            "pokemon/25/" or "pokemon/25" => MockHttpMessageHandler.JsonResponse(
                PokeApiFixtures.PokemonDetail(25, "electric")),
            "type/fire/" or "type/fire" => MockHttpMessageHandler.JsonResponse(PokeApiFixtures.FireTypeDetail()),
            "ability/overgrow/" or "ability/overgrow" => MockHttpMessageHandler.JsonResponse(PokeApiFixtures.OvergrowAbility()),
            "generation/1/" or "generation/1" or "generation/generation-i/" or "generation/generation-i"
                => MockHttpMessageHandler.JsonResponse(PokeApiFixtures.GenerationOneDetail()),
            "generation/generation-ii/" or "generation/generation-ii"
                => MockHttpMessageHandler.JsonResponse(PokeApiFixtures.GenerationTwoDetail()),
            _ => MockHttpMessageHandler.NotFound()
        };
    }

    private static int ParseOffset(string query) {
        var match = System.Text.RegularExpressions.Regex.Match(query, @"offset=(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}
