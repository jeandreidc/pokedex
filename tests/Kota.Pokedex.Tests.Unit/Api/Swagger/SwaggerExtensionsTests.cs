using Kota.Pokedex.Api.Swagger;
using Microsoft.Extensions.DependencyInjection;

namespace Kota.Pokedex.Tests.Unit.Api.Swagger;

public class SwaggerExtensionsTests {
    [Fact]
    public void AddSwaggerDocumentation_RegistersSwaggerGen() {
        var services = new ServiceCollection();

        services.AddSwaggerDocumentation();

        services.Should().Contain(d => d.ServiceType.Name.Contains("SwaggerGen", StringComparison.Ordinal));
    }
}
