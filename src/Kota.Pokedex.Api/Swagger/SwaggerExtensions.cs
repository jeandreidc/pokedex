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
                    Backend API for the Pokedex — search/filter Pokemon and manage per-user collections.

                    Public endpoints proxy data from [PokeAPI](https://pokeapi.co/docs/v2).
                    Collection endpoints require JWT Bearer authentication.
                    """
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
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
