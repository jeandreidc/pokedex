using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kota.Pokedex.Tests.Unit.Infrastructure.Services;

public class PokemonPrefetchHostedServiceTests {
    [Fact]
    public async Task StartAsync_TriggersWarmupAfterApplicationStarted() {
        var indexMock = new Mock<IPokemonIndexService>();
        var filterMock = new Mock<IFilterMetadataService>();
        var warmupState = new WarmupState();
        var startedCts = new CancellationTokenSource();

        var services = new ServiceCollection();
        services.AddScoped(_ => indexMock.Object);
        services.AddScoped(_ => filterMock.Object);
        var provider = services.BuildServiceProvider();

        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        lifetimeMock.Setup(l => l.ApplicationStarted).Returns(startedCts.Token);
        lifetimeMock.Setup(l => l.ApplicationStopping).Returns(CancellationToken.None);

        var sut = new PokemonPrefetchHostedService(
            provider,
            lifetimeMock.Object,
            warmupState,
            NullLogger<PokemonPrefetchHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await startedCts.CancelAsync();

        await Task.Delay(50);

        indexMock.Verify(s => s.WarmupAsync(It.IsAny<CancellationToken>()), Times.Once);
        filterMock.Verify(s => s.WarmupAsync(It.IsAny<CancellationToken>()), Times.Once);
        indexMock.Verify(
            s => s.PrefetchFirstPageCardDetailsAsync(PokemonPrefetchHostedService.DefaultFirstPagePrefetchSize, It.IsAny<CancellationToken>()),
            Times.Once);
        warmupState.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_LogsError_WhenWarmupFails() {
        var indexMock = new Mock<IPokemonIndexService>();
        indexMock.Setup(s => s.WarmupAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("warmup failed"));

        var warmupState = new WarmupState();
        var startedCts = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddScoped(_ => indexMock.Object);
        services.AddScoped(_ => new Mock<IFilterMetadataService>().Object);
        var provider = services.BuildServiceProvider();

        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        lifetimeMock.Setup(l => l.ApplicationStarted).Returns(startedCts.Token);
        lifetimeMock.Setup(l => l.ApplicationStopping).Returns(CancellationToken.None);

        var sut = new PokemonPrefetchHostedService(
            provider,
            lifetimeMock.Object,
            warmupState,
            NullLogger<PokemonPrefetchHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await startedCts.CancelAsync();
        await Task.Delay(50);

        indexMock.Verify(s => s.WarmupAsync(It.IsAny<CancellationToken>()), Times.Once);
        warmupState.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutError() {
        var provider = new ServiceCollection().BuildServiceProvider();
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        lifetimeMock.Setup(l => l.ApplicationStarted).Returns(CancellationToken.None);
        lifetimeMock.Setup(l => l.ApplicationStopping).Returns(CancellationToken.None);

        var sut = new PokemonPrefetchHostedService(
            provider,
            lifetimeMock.Object,
            new WarmupState(),
            NullLogger<PokemonPrefetchHostedService>.Instance);

        await sut.StopAsync(CancellationToken.None);
    }
}
