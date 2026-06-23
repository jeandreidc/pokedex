using Kota.Pokedex.Application.Queries.Pokemon;
using Kota.Pokedex.Tests.Unit.Helpers.Mocks;

namespace Kota.Pokedex.Tests.Unit.Application.Queries.Pokemon;

public class SearchPokemonQueryHandlerTests {
    private readonly SearchPokemonQueryHandler _sut;

    public SearchPokemonQueryHandlerTests() {
        _sut = new SearchPokemonQueryHandler(PokemonIndexServiceMock.CreateDefault().Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResults_WithDefaults() {
        var result = await _sut.Handle(new SearchPokemonQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Should().HaveCount(20);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ReturnsSecondPage() {
        var result = await _sut.Handle(new SearchPokemonQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

        result.Items.Should().HaveCount(10);
        result.Items[0].Id.Should().Be(11);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_FiltersBySearchTerm() {
        var result = await _sut.Handle(new SearchPokemonQuery { Search = "char" }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Select(i => i.Name).Should().BeEquivalentTo(["charmander", "charmeleon", "charizard"]);
    }

    [Fact]
    public async Task Handle_FiltersByType() {
        var result = await _sut.Handle(new SearchPokemonQuery { Type = "fire" }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().OnlyContain(i => i.Types.Contains("fire"));
    }

    [Fact]
    public async Task Handle_FiltersByAbility() {
        var result = await _sut.Handle(new SearchPokemonQuery { Ability = "overgrow" }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Select(i => i.Id).Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task Handle_FiltersByGeneration() {
        var result = await _sut.Handle(new SearchPokemonQuery { Generation = "1" }, CancellationToken.None);

        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task Handle_IntersectsMultipleFilters() {
        var result = await _sut.Handle(new SearchPokemonQuery {
            Type = "fire",
            Ability = "overgrow"
        }, CancellationToken.None);

        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ClampsPageSizeToMax100() {
        var result = await _sut.Handle(new SearchPokemonQuery { PageSize = 500 }, CancellationToken.None);

        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Handle_NormalizesPageToMinimumOne() {
        var result = await _sut.Handle(new SearchPokemonQuery { Page = 0, PageSize = 5 }, CancellationToken.None);

        result.Page.Should().Be(1);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_OrdersResultsById() {
        var result = await _sut.Handle(new SearchPokemonQuery { PageSize = 25 }, CancellationToken.None);

        result.Items.Select(i => i.Id).Should().BeInAscendingOrder();
    }
}
