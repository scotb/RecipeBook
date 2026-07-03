using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using RecipeBook.Application.Exceptions;
using RecipeBook.Api;

namespace RecipeBook.Api.Tests;

public class GlobalExceptionHandlerTests
{
    private static async Task<(int StatusCode, string ContentType, JsonElement Body)> InvokeHandlerAsync(Exception ex)
    {
        using var ms = new MemoryStream();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = ms;

        var handler = new GlobalExceptionHandler();
        await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

        var bodyBytes = ms.ToArray();
        var body = JsonSerializer.Deserialize<JsonElement>(bodyBytes)!;

        return (ctx.Response.StatusCode, ctx.Response.ContentType ?? "", body);
    }

    [Fact]
    public async Task MapsNotFoundException_Returns404ProblemDetails()
    {
        // Act
        var (status, contentType, body) = await InvokeHandlerAsync(new NotFoundException("Recipe not found"));

        // Assert
        status.Should().Be(404);
        contentType.Should().Contain("application/problem+json");
        body.GetProperty("type").GetString().Should().Be("not-found");
        body.GetProperty("title").GetString().Should().Be("Not Found");
        body.GetProperty("status").GetInt32().Should().Be(404);
        body.GetProperty("detail").GetString().Should().Be("Recipe not found");
    }

    [Fact]
    public async Task MapsForbiddenException_Returns403ProblemDetails()
    {
        // Act
        var (status, _, body) = await InvokeHandlerAsync(new ForbiddenException("Access denied"));

        // Assert
        status.Should().Be(403);
        body.GetProperty("type").GetString().Should().Be("forbidden");
        body.GetProperty("title").GetString().Should().Be("Forbidden");
        body.GetProperty("status").GetInt32().Should().Be(403);
    }

    [Fact]
    public async Task MapsConflictException_Returns409ProblemDetails()
    {
        // Act
        var (status, _, body) = await InvokeHandlerAsync(new ConflictException("Already exists"));

        // Assert
        status.Should().Be(409);
        body.GetProperty("type").GetString().Should().Be("conflict");
        body.GetProperty("title").GetString().Should().Be("Conflict");
        body.GetProperty("status").GetInt32().Should().Be(409);
    }

    [Fact]
    public async Task MapsArgumentException_Returns400ProblemDetails()
    {
        var (status, contentType, body) = await InvokeHandlerAsync(new ArgumentException("Bad URL"));

        status.Should().Be(400);
        contentType.Should().Contain("application/problem+json");
        body.GetProperty("type").GetString().Should().Be("bad-request");
        body.GetProperty("title").GetString().Should().Be("Bad Request");
        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("detail").GetString().Should().Be("Bad URL");
    }

    [Fact]
    public async Task MapsInvalidImportException_Returns422ProblemDetails()
    {
        // Act
        var (status, _, body) = await InvokeHandlerAsync(new InvalidImportException("Bad import"));

        // Assert
        status.Should().Be(422);
        body.GetProperty("type").GetString().Should().Be("unprocessable-entity");
        body.GetProperty("title").GetString().Should().Be("Unprocessable Entity");
        body.GetProperty("status").GetInt32().Should().Be(422);
    }

    [Fact]
    public async Task MapsUnknownException_Returns500ProblemDetails()
    {
        // Act
        var (status, _, body) = await InvokeHandlerAsync(new InvalidOperationException("Something broke"));

        // Assert
        status.Should().Be(500);
        body.GetProperty("type").GetString().Should().Be("internal-error");
        body.GetProperty("title").GetString().Should().Be("Internal Server Error");
        body.GetProperty("status").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task MapsUnknownException_ReturnsGenericDetail_NotExMessage()
    {
        // Act
        var (status, _, body) = await InvokeHandlerAsync(new InvalidOperationException("Database connection failed at line 42"));

        // Assert — SEC-004: 500 errors must not leak internal details
        status.Should().Be(500);
        var detail = body.GetProperty("detail").GetString();
        detail.Should().NotBe("Database connection failed at line 42");
        detail.Should().Contain("unexpected");
    }
}
