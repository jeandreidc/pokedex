using System.Text.Json;
using Kota.Pokedex.Api.Middleware;
using Kota.Pokedex.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kota.Pokedex.Tests.Unit.Api.Middleware;

public class ExceptionHandlingMiddlewareTests {
    [Fact]
    public async Task InvokeAsync_PassesThrough_WhenNoException() {
        var context = new DefaultHttpContext();
        var called = false;
        RequestDelegate next = _ => {
            called = true;
            return Task.CompletedTask;
        };

        var sut = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ReturnsBadGateway_ForPokeApiException() {
        var context = CreateContext();
        RequestDelegate next = _ => throw new PokeApiException("upstream failed", 502);

        var sut = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(502);
        await AssertErrorBody(context, "upstream failed");
    }

    [Fact]
    public async Task InvokeAsync_ReturnsBadRequest_ForArgumentException() {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ArgumentException("invalid arg");

        var sut = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        await AssertErrorBody(context, "invalid arg");
    }

    [Fact]
    public async Task InvokeAsync_Returns500_ForUnexpectedException() {
        var context = CreateContext();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");

        var sut = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        await AssertErrorBody(context, "An unexpected error occurred.");
    }

    private static DefaultHttpContext CreateContext() {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task AssertErrorBody(HttpContext context, string expectedMessage) {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("error").GetString().Should().Be(expectedMessage);
    }
}
