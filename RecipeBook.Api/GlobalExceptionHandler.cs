using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Application.Exceptions;

namespace RecipeBook.Api;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        (int status, string type, string title) = ex switch
        {
            NotFoundException => (404, "not-found", "Not Found"),
            ForbiddenException => (403, "forbidden", "Forbidden"),
            ConflictException => (409, "conflict", "Conflict"),
            InvalidImportException => (422, "unprocessable-entity", "Unprocessable Entity"),
            ArgumentException => (400, "bad-request", "Bad Request"),
            _ => (500, "internal-error", "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = ex.Message
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOpts), ct);
        return true;
    }
}
