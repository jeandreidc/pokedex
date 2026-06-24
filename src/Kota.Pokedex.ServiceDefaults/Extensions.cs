using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions {
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string ReadinessEndpointPath = "/health/ready";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder {
        builder.ConfigureJsonLogging();
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http => {
            http.AddStandardResilienceHandler();
        });
        return builder;
    }

    public static TBuilder ConfigureJsonLogging<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options => {
            options.IncludeScopes = true;
            options.TimestampFormat = "O";
            options.UseUtcTimestamp = true;
            options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions {
                Indented = false
            };
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder {
        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>(null, LogLevel.Information);

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Kota.Pokedex");
            })
            .WithTracing(tracing => {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])) {
            openTelemetry.UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app) {
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions {
            Predicate = r => r.Tags.Contains("live")
        });
        app.MapHealthChecks(ReadinessEndpointPath, new HealthCheckOptions {
            Predicate = r => r.Tags.Contains("ready")
        });

        return app;
    }
}
