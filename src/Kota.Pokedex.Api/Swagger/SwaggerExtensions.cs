using Microsoft.OpenApi;

namespace Kota.Pokedex.Api.Swagger;

public static class SwaggerExtensions {
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services) {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => {
            options.SwaggerDoc("v1", new OpenApiInfo {
                Title = "Kota Pokedex API",
                Version = "v1",
                Description = """
                    Backend API for the Pokedex take-home exercise (Feature #2: Search & Filter with Pagination).

                    All endpoints proxy and normalize data from [PokeAPI](https://pokeapi.co/docs/v2).
                    Filters are combinable — use `search`, `type`, `ability`, and `generation` together on `GET /api/pokemon`.
                    """
            });

            var xmlPath = Path.Combine(AppContext.BaseDirectory, "Kota.Pokedex.Api.xml");
            if (File.Exists(xmlPath)) {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app) {
        app.UseSwagger();
        app.UseSwaggerUI(options => {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kota Pokedex API v1");
            options.DocumentTitle = "Kota Pokedex API";
            options.RoutePrefix = string.Empty;
        });

        return app;
    }
}
