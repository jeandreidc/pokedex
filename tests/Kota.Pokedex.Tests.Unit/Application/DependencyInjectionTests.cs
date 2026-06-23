using Kota.Pokedex.Application;
using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Pokemon;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Kota.Pokedex.Tests.Unit.Application;

public class DependencyInjectionTests {
    [Fact]
    public void AddApplication_RegistersMediatR() {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
        services.Should().Contain(d => d.ServiceType == typeof(IMediator));
        services.Should().Contain(d =>
            d.ImplementationType == typeof(SearchPokemonQueryHandler));
    }
}
