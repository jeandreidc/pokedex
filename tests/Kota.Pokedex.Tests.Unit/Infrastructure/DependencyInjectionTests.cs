using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Infrastructure;
using Kota.Pokedex.Infrastructure.Caching;
using Kota.Pokedex.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kota.Pokedex.Tests.Unit.Infrastructure;

public class DependencyInjectionTests {
    [Fact]
    public void AddInfrastructure_RegistersMemoryCache_WhenProviderIsMemory() {
        var services = new ServiceCollection();
        var config = BuildConfig("Memory");

        services.AddInfrastructure(config);
        var provider = services.BuildServiceProvider();

        provider.GetService<ICacheService>()
            .Should().BeOfType<MemoryCacheService>();
    }

    [Fact]
    public void AddInfrastructure_RegistersRedisCache_WhenProviderIsRedis() {
        var services = new ServiceCollection();
        var config = BuildConfig("Redis");

        services.AddInfrastructure(config);

        services.Should().Contain(d =>
            d.ServiceType == typeof(ICacheService) &&
            d.ImplementationType == typeof(RedisCacheService));
    }

    [Fact]
    public void AddInfrastructure_RegistersCoreServices() {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig("Memory"));

        services.Should().Contain(d => d.ServiceType == typeof(IPokemonIndexService));
        services.Should().Contain(d => d.ServiceType == typeof(IFilterMetadataService));
        services.Should().Contain(d => d.ImplementationType == typeof(PokemonPrefetchHostedService));
    }

    private static IConfiguration BuildConfig(string cacheProvider) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cache:Provider"] = cacheProvider,
                ["Cache:DefaultTtlMinutes"] = "60",
                ["PokeApi:BaseUrl"] = "https://pokeapi.co/api/v2/",
                ["ConnectionStrings:redis"] = "localhost:6379"
            })
            .Build();
}
