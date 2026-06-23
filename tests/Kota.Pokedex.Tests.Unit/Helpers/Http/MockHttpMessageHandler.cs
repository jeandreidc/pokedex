using System.Net;
using System.Text;
using System.Text.Json;
using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kota.Pokedex.Tests.Unit.Helpers.Http;

public sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    : HttpMessageHandler {
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        Requests.Add(request);
        return Task.FromResult(handler(request));
    }

    public static HttpResponseMessage JsonResponse<T>(T payload, HttpStatusCode status = HttpStatusCode.OK) {
        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage(status) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage NotFound() =>
        new(HttpStatusCode.NotFound);

    public static HttpClient CreateClient(string baseUrl, Func<HttpRequestMessage, HttpResponseMessage> handler) {
        var httpHandler = new MockHttpMessageHandler(handler);
        return new HttpClient(httpHandler) { BaseAddress = new Uri(baseUrl) };
    }
}

public static class TestOptions {
    public static IOptions<T> Create<T>(T value) where T : class, new() =>
        Options.Create(value);

    public static IOptions<PokeApiOptions> PokeApi(int pageFetchLimit = 10, int maxConcurrent = 5) =>
        Create(new PokeApiOptions {
            BaseUrl = "https://pokeapi.co/api/v2/",
            PageFetchLimit = pageFetchLimit,
            MaxConcurrentRequests = maxConcurrent
        });

    public static IOptions<CacheOptions> Cache(int ttlMinutes = 60) =>
        Create(new CacheOptions { DefaultTtlMinutes = ttlMinutes, Provider = "Memory" });
}
