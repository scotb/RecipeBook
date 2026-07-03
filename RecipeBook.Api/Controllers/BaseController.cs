using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace RecipeBook.Api.Controllers;

/// <summary>
/// Base controller providing shared helpers for authenticated API controllers.
/// </summary>
public abstract class BaseController : ControllerBase
{
    protected string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");
        return userId;
    }

    protected IActionResult Problem(int statusCode, string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Detail = detail
        })
        {
            StatusCode = statusCode
        };
    }
}
