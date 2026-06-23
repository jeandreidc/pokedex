using Kota.Pokedex.Application.Queries.Filters;
using Kota.Pokedex.Application.Queries.Pokemon;
using Microsoft.Extensions.DependencyInjection;

namespace Kota.Pokedex.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SearchPokemonQuery).Assembly));

        return services;
    }
}
