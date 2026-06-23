using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Kota.Pokedex.Tests.Integration.Support;

public sealed class PokedexWebApplicationFactory : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) => {
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["RateLimiting:PermitLimit"] = "10000",
                ["RateLimiting:WindowMinutes"] = "1",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
            });
        });

        builder.ConfigureTestServices(services => {
            var prefetchDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(PokemonPrefetchHostedService))
                .ToList();

            foreach (var descriptor in prefetchDescriptors) {
                services.Remove(descriptor);
            }

            services.RemoveAll<IPokeApiClient>();
            services.AddSingleton<IPokeApiClient, FakePokeApiClient>();
        });
    }
}

[CollectionDefinition(nameof(PokedexIntegrationCollection))]
public sealed class PokedexIntegrationCollection : ICollectionFixture<PokedexWebApplicationFactory>;
