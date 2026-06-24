using Kota.Pokedex.Application.Interfaces;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Options;
using Kota.Pokedex.Infrastructure.Caching;
using Kota.Pokedex.Infrastructure.ExternalServices.PokeApi;
using Kota.Pokedex.Infrastructure.Health;
using Kota.Pokedex.Infrastructure.Metrics;
using Kota.Pokedex.Infrastructure.Persistence;
using Kota.Pokedex.Infrastructure.Repositories;
using Kota.Pokedex.Infrastructure.Security;
using Kota.Pokedex.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kota.Pokedex.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<PokeApiOptions>(configuration.GetSection(PokeApiOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("PokedexDb")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPokedexMetricsService, PokedexMetricsService>();

        var cacheProvider = configuration.GetSection(CacheOptions.SectionName)["Provider"] ?? "Memory";

        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase)) {
            services.AddStackExchangeRedisCache(options => {
                options.Configuration = configuration.GetConnectionString("redis");
            });
            services.AddSingleton<ICacheService, RedisCacheService>();
        } else {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        services.AddHttpClient<IPokeApiClient, PokeApiClient>((sp, client) => {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PokeApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IWarmupState, WarmupState>();
        services.AddSingleton<IPokemonIndexService, PokemonIndexService>();
        services.AddSingleton<IFilterMetadataService, FilterMetadataService>();
        services.AddHostedService<PokemonPrefetchHostedService>();

        services.AddHealthChecks()
            .AddCheck<WarmupHealthCheck>("warmup", tags: ["ready"]);

        return services;
    }
}
