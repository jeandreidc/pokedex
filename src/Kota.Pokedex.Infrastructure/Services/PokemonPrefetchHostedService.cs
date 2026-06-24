using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kota.Pokedex.Infrastructure.Services;

public class PokemonPrefetchHostedService : IHostedService {
    public const int DefaultFirstPagePrefetchSize = PokemonPagination.CatalogPageSize;

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IWarmupState _warmupState;
    private readonly ILogger<PokemonPrefetchHostedService> _logger;

    public PokemonPrefetchHostedService(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime lifetime,
        IWarmupState warmupState,
        ILogger<PokemonPrefetchHostedService> logger) {
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
        _warmupState = warmupState;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        _lifetime.ApplicationStarted.Register(() => {
            _ = WarmupAsync(_lifetime.ApplicationStopping);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WarmupAsync(CancellationToken cancellationToken) {
        try {
            using var scope = _serviceProvider.CreateScope();
            var indexService = scope.ServiceProvider.GetRequiredService<IPokemonIndexService>();
            var filterMetadata = scope.ServiceProvider.GetRequiredService<IFilterMetadataService>();

            await indexService.WarmupAsync(cancellationToken);
            await filterMetadata.WarmupAsync(cancellationToken);
            await indexService.PrefetchFirstPageCardDetailsAsync(DefaultFirstPagePrefetchSize, cancellationToken);
            _warmupState.MarkComplete();
            _logger.LogInformation("Startup prefetch complete");
        }
        catch (OperationCanceledException) {
            _logger.LogInformation("Startup prefetch cancelled during {Phase}", "warmup");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Startup prefetch failed during {Phase}", "warmup");
        }
    }
}
