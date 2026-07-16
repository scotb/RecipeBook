using System.Text.Json;

namespace RecipeBook.Api.Middleware;

public sealed class UnauthorizedHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public UnauthorizedHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        await _next(ctx);

        if (ctx.Response.StatusCode == 401 && !ctx.Response.HasStarted)
        {
            ctx.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = "authentication-failed",
                title = "Unauthorized",
                status = 401,
                detail = "Authentication required. Please provide a valid JWT token."
            };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
