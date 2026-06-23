using Kota.Pokedex.Application.Queries.Filters;
using Kota.Pokedex.Tests.Unit.Fixtures.Filters;
using Kota.Pokedex.Tests.Unit.Helpers.Mocks;

namespace Kota.Pokedex.Tests.Unit.Application.Queries.Filters;

public class GetFilterTypesQueryHandlerTests {
    [Fact]
    public async Task Handle_ReturnsAllTypes() {
        var sut = new GetFilterTypesQueryHandler(FilterMetadataServiceMock.CreateDefault().Object);

        var result = await sut.Handle(new GetFilterTypesQuery(), CancellationToken.None);

        result.Should().HaveCount(FilterFixtures.Types.Count);
        result.Should().Contain(t => t.Name == "fire" && t.DisplayName == "Fire");
    }
}

public class GetFilterGenerationsQueryHandlerTests {
    [Fact]
    public async Task Handle_ReturnsAllGenerations() {
        var sut = new GetFilterGenerationsQueryHandler(FilterMetadataServiceMock.CreateDefault().Object);

        var result = await sut.Handle(new GetFilterGenerationsQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].DisplayName.Should().Be("Generation I");
    }
}

public class GetFilterAbilitiesQueryHandlerTests {
    private readonly GetFilterAbilitiesQueryHandler _sut;

    public GetFilterAbilitiesQueryHandlerTests() {
        _sut = new GetFilterAbilitiesQueryHandler(FilterMetadataServiceMock.CreateDefault().Object);
    }

    [Fact]
    public async Task Handle_ReturnsFirstPage() {
        var result = await _sut.Handle(new GetFilterAbilitiesQuery { Page = 1, PageSize = 5 }, CancellationToken.None);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ReturnsSecondPage() {
        var result = await _sut.Handle(new GetFilterAbilitiesQuery { Page = 2, PageSize = 5 }, CancellationToken.None);

        result.Items.Should().HaveCount(5);
        result.Items[0].Name.Should().Be("ability-15");
    }

    [Fact]
    public async Task Handle_FiltersBySearchTerm() {
        var result = await _sut.Handle(new GetFilterAbilitiesQuery { Search = "over" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("overgrow");
    }

    [Fact]
    public async Task Handle_ClampsPageSize() {
        var result = await _sut.Handle(new GetFilterAbilitiesQuery { PageSize = 200 }, CancellationToken.None);

        result.PageSize.Should().Be(100);
    }
}
