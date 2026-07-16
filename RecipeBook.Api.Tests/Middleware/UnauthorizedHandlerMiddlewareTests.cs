using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using RecipeBook.Api.Middleware;

namespace RecipeBook.Api.Tests.Middleware;

public class UnauthorizedHandlerMiddlewareTests
{
    private static async Task<(int StatusCode, string? ContentType, string Body)> InvokeWithStatusAsync(int inputStatus)
    {
        using var ms = new MemoryStream();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = ms;
        ctx.Response.StatusCode = inputStatus;

        Task Next(HttpContext _) => Task.CompletedTask;
        var middleware = new UnauthorizedHandlerMiddleware(Next);
        await middleware.InvokeAsync(ctx);

        var bodyBytes = ms.ToArray();
        var body = System.Text.Encoding.UTF8.GetString(bodyBytes);

        return (ctx.Response.StatusCode, ctx.Response.ContentType, body);
    }

    [Fact]
    public async Task InvokeAsync_401Response_ConvertsToProblemDetails()
    {
        // Act
        var (status, contentType, body) = await InvokeWithStatusAsync(401);

        // Assert
        status.Should().Be(401);
        contentType.Should().Contain("application/problem+json");
        var json = JsonSerializer.Deserialize<JsonElement>(body)!;
        json.GetProperty("type").GetString().Should().Be("authentication-failed");
        json.GetProperty("title").GetString().Should().Be("Unauthorized");
        json.GetProperty("status").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_200Response_Unchanged()
    {
        // Act
        var (status, contentType, _) = await InvokeWithStatusAsync(200);

        // Assert
        status.Should().Be(200);
        contentType.Should().BeNull();
    }
}
