using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Filters;
using Kota.Pokedex.Application.Queries.Pokemon;
using Kota.Pokedex.Api.Controllers;
using MediatR;

namespace Kota.Pokedex.Tests.Unit.Api.Controllers;

public class PokemonControllerTests {
    private readonly Mock<IMediator> _mediator = new();
    private readonly PokemonController _sut;

    public PokemonControllerTests() {
        _sut = new PokemonController(_mediator.Object);
    }

    [Fact]
    public async Task Search_ReturnsOkWithPagedResult() {
        var expected = new PagedResult<PokemonSummaryDto> {
            Items = [new PokemonSummaryDto { Id = 25, Name = "pikachu" }],
            Page = 1,
            PageSize = 20,
            TotalCount = 1
        };
        _mediator.Setup(m => m.Send(It.IsAny<SearchPokemonQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Search(null, null, null, null, 1, 20, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Search_ForwardsQueryParameters() {
        SearchPokemonQuery? captured = null;
        _mediator.Setup(m => m.Send(It.IsAny<SearchPokemonQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<PokemonSummaryDto>>, CancellationToken>((q, _) => captured = (SearchPokemonQuery)q)
            .ReturnsAsync(new PagedResult<PokemonSummaryDto>());

        await _sut.Search("pi", "fire", "overgrow", "1", 2, 10, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Search.Should().Be("pi");
        captured.Type.Should().Be("fire");
        captured.Ability.Should().Be("overgrow");
        captured.Generation.Should().Be("1");
        captured.Page.Should().Be(2);
        captured.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound() {
        var dto = new PokemonSummaryDto { Id = 25, Name = "pikachu" };
        _mediator.Setup(m => m.Send(It.IsAny<GetPokemonByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.GetById("pikachu", CancellationToken.None);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing() {
        _mediator.Setup(m => m.Send(It.IsAny<GetPokemonByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PokemonSummaryDto?)null);

        var result = await _sut.GetById("missing", CancellationToken.None);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.NotFoundResult>();
    }
}

public class FiltersControllerTests {
    private readonly Mock<IMediator> _mediator = new();
    private readonly FiltersController _sut;

    public FiltersControllerTests() {
        _sut = new FiltersController(_mediator.Object);
    }

    [Fact]
    public async Task GetTypes_ReturnsOk() {
        _mediator.Setup(m => m.Send(It.IsAny<GetFilterTypesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FilterOptionDto> { new() { Name = "fire" } });

        var result = await _sut.GetTypes(CancellationToken.None);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
    }

    [Fact]
    public async Task GetAbilities_ForwardsPaginationAndSearch() {
        GetFilterAbilitiesQuery? captured = null;
        _mediator.Setup(m => m.Send(It.IsAny<GetFilterAbilitiesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<FilterOptionDto>>, CancellationToken>((q, _) => captured = (GetFilterAbilitiesQuery)q)
            .ReturnsAsync(new PagedResult<FilterOptionDto>());

        await _sut.GetAbilities("over", 2, 25, CancellationToken.None);

        captured!.Search.Should().Be("over");
        captured.Page.Should().Be(2);
        captured.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task GetGenerations_ReturnsOk() {
        _mediator.Setup(m => m.Send(It.IsAny<GetFilterGenerationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FilterOptionDto>());

        var result = await _sut.GetGenerations(CancellationToken.None);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
    }
}
