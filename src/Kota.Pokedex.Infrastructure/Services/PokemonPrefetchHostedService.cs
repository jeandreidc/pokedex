using System.Diagnostics;
using Kota.Pokedex.Core.Constants;
using Kota.Pokedex.Core.Diagnostics;
using Kota.Pokedex.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kota.Pokedex.Infrastructure.Services;

public class PokemonPrefetchHostedService : IHostedService {
    public const int DefaultFirstPagePrefetchSize = PokemonPagination.CatalogPageSize;

    private static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

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
        using var activity = PokedexActivitySources.Source.StartActivity("Startup.Warmup");
        var attempt = 0;
        var backoff = InitialBackoff;

        while (!cancellationToken.IsCancellationRequested) {
            attempt++;
            try {
                using var scope = _serviceProvider.CreateScope();
                var indexService = scope.ServiceProvider.GetRequiredService<IPokemonIndexService>();
                var filterMetadata = scope.ServiceProvider.GetRequiredService<IFilterMetadataService>();

                await indexService.WarmupAsync(cancellationToken);
                await filterMetadata.WarmupAsync(cancellationToken);
                await indexService.PrefetchFirstPageCardDetailsAsync(DefaultFirstPagePrefetchSize, cancellationToken);
                _warmupState.MarkComplete();
                _logger.LogInformation("Startup prefetch complete after {Attempt} attempt(s)", attempt);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                _logger.LogInformation("Startup prefetch cancelled during {Phase}", "warmup");
                activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
                return;
            }
            catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "Startup prefetch failed on attempt {Attempt}; retrying in {DelaySeconds}s",
                    attempt,
                    backoff.TotalSeconds);
                activity?.AddEvent(new ActivityEvent(
                    "warmup.retry",
                    tags: new ActivityTagsCollection {
                        { "attempt", attempt },
                        { "delay_seconds", backoff.TotalSeconds }
                    }));

                try {
                    await Task.Delay(backoff, cancellationToken);
                }
                catch (OperationCanceledException) {
                    _logger.LogInformation("Startup prefetch cancelled during {Phase}", "backoff");
                    activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
                    return;
                }

                var nextMs = Math.Min(backoff.TotalMilliseconds * 2, MaxBackoff.TotalMilliseconds);
                backoff = TimeSpan.FromMilliseconds(nextMs);
            }
        }
    }
}
