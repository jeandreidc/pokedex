using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Core.Exceptions;

namespace Kota.Pokedex.Tests.Unit.Core;

public class CacheKeysTests {
    [Theory]
    [InlineData("Fire", "pokeapi:type:fire")]
    [InlineData("OVERGROW", "pokeapi:ability:overgrow")]
    [InlineData("Generation-I", "pokeapi:generation:generation-i")]
    public void KeyBuilders_NormalizeToLowercase(string input, string expected) {
        if (expected.Contains("type:")) {
            CacheKeys.Type(input).Should().Be(expected);
        }
        else if (expected.Contains("ability:")) {
            CacheKeys.Ability(input).Should().Be(expected);
        }
        else {
            CacheKeys.Generation(input).Should().Be(expected);
        }
    }

    [Fact]
    public void PokemonDetail_IncludesId() {
        CacheKeys.PokemonDetail(25).Should().Be("pokeapi:pokemon:25");
    }
}

public class PagedResultTests {
    [Theory]
    [InlineData(25, 10, 3)]
    [InlineData(20, 10, 2)]
    [InlineData(0, 10, 0)]
    public void TotalPages_CalculatesCorrectly(int totalCount, int pageSize, int expectedPages) {
        var result = new PagedResult<string> {
            TotalCount = totalCount,
            PageSize = pageSize
        };

        result.TotalPages.Should().Be(expectedPages);
    }
}

public class PokeApiExceptionTests {
    [Fact]
    public void StoresStatusCodeAndMessage() {
        var ex = new PokeApiException("failed", 404);

        ex.Message.Should().Be("failed");
        ex.StatusCode.Should().Be(404);
    }
}
