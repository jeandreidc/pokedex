using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Bootstrap;
using Kota.Pokedex.Application.Queries.Filters;
using Kota.Pokedex.Application.Queries.Pokemon;
using Kota.Pokedex.Application.Common;
using MediatR;

namespace Kota.Pokedex.Tests.Unit.Application.Queries.Bootstrap;

public class GetBootstrapQueryHandlerTests {
    private readonly Mock<IMediator> _mediator = new();
    private readonly GetBootstrapQueryHandler _sut;

    public GetBootstrapQueryHandlerTests() {
        _sut = new GetBootstrapQueryHandler(_mediator.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFirstPageWithTotalCounts() {
        _mediator.Setup(m => m.Send(It.IsAny<GetFilterTypesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FilterOptionDto> { new() { Name = "fire", DisplayName = "Fire" } });
        _mediator.Setup(m => m.Send(It.IsAny<GetFilterGenerationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FilterOptionDto> { new() { Id = 1, Name = "generation-i", DisplayName = "Generation I" } });
        _mediator.Setup(m => m.Send(It.IsAny<GetFilterAbilitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<FilterOptionDto> {
                Items = [new FilterOptionDto { Name = "overgrow", DisplayName = "Overgrow" }],
                Page = 1,
                PageSize = 50,
                TotalCount = 367
            });
        _mediator.Setup(m => m.Send(It.IsAny<SearchPokemonQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PokemonSummaryDto> {
                Items = [new PokemonSummaryDto { Id = 1, Name = "bulbasaur" }],
                Page = 1,
                PageSize = 12,
                TotalCount = 1350
            });

        var result = await _sut.Handle(new GetBootstrapQuery { PageSize = 12 }, CancellationToken.None);

        result.Types.Should().HaveCount(1);
        result.Generations.Should().HaveCount(1);
        result.Abilities.TotalCount.Should().Be(367);
        result.Abilities.Items.Should().HaveCount(1);
        result.Pokemon.TotalCount.Should().Be(1350);
        result.Pokemon.TotalPages.Should().Be(113);
        result.Pokemon.Items.Should().HaveCount(1);
    }
}
