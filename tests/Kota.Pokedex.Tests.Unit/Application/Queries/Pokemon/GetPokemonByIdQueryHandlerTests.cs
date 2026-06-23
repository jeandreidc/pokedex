using Kota.Pokedex.Application.Queries.Pokemon;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models;
using Kota.Pokedex.Tests.Unit.Fixtures.Index;
using Kota.Pokedex.Tests.Unit.Helpers.Mocks;

namespace Kota.Pokedex.Tests.Unit.Application.Queries.Pokemon;

public class GetPokemonByIdQueryHandlerTests {
    private readonly GetPokemonByIdQueryHandler _sut;

    public GetPokemonByIdQueryHandlerTests() {
        _sut = new GetPokemonByIdQueryHandler(PokemonIndexServiceMock.CreateDefault().Object);
    }

    [Fact]
    public async Task Handle_ReturnsPokemon_WhenFoundByName() {
        var result = await _sut.Handle(new GetPokemonByIdQuery("pikachu"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(25);
        result.Name.Should().Be("pikachu");
        result.Types.Should().Contain("electric");
    }

    [Fact]
    public async Task Handle_ReturnsPokemon_WhenFoundById() {
        var result = await _sut.Handle(new GetPokemonByIdQuery("4"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("charmander");
        result.Types.Should().Contain("fire");
    }

    [Fact]
    public async Task Handle_IsCaseInsensitiveForName() {
        var result = await _sut.Handle(new GetPokemonByIdQuery("PIKACHU"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(25);
    }

    [Fact]
    public async Task Handle_UsesGetEntryAsync_WhenNotInIndexByName() {
        var mock = new Mock<IPokemonIndexService>();
        mock.Setup(s => s.GetIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PokemonIndexEntry>());
        mock.Setup(s => s.GetEntryAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PokemonIndexFixtures.CreateEntry(25));
        mock.Setup(s => s.GetTypesForPokemonAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "electric" });

        var sut = new GetPokemonByIdQueryHandler(mock.Object);
        var result = await sut.Handle(new GetPokemonByIdQuery("25"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("pikachu");
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenNotFound() {
        var result = await _sut.Handle(new GetPokemonByIdQuery("missing-mon"), CancellationToken.None);

        result.Should().BeNull();
    }
}
