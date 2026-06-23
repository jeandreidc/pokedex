using System.Threading.RateLimiting;
using Kota.Pokedex.Application;
using Kota.Pokedex.Core.Options;
using Kota.Pokedex.Infrastructure;
using Kota.Pokedex.Api.Middleware;
using Kota.Pokedex.Api.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));

var rateLimitOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions {
                PermitLimit = rateLimitOptions.PermitLimit,
                Window = TimeSpan.FromMinutes(rateLimitOptions.WindowMinutes),
                QueueLimit = 0
            }));
});

builder.Services.AddControllers();
builder.Services.AddCors(options => {
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwaggerDocumentation();
app.UseExceptionHandling();
app.UseRateLimiter();
app.UseCors("Frontend");
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
