using FluentValidation;
using Kota.Pokedex.Application.Behaviors;
using Kota.Pokedex.Application.Commands.Auth;
using Kota.Pokedex.Application.Queries.Pokemon;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Kota.Pokedex.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SearchPokemonQuery).Assembly));

        services.AddValidatorsFromAssembly(typeof(RegisterUserCommandValidator).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
