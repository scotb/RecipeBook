using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;

namespace RecipeBook.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public AdminController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpGet("recipes")]
    public async Task<IActionResult> GetRecipes(
        [FromQuery] string? search = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = RequireUserId();

        if (page < 1 || pageSize < 1 || pageSize > 100)
            return Problem(400, "Page and pageSize must be >= 1, pageSize must be <= 100.");

        if (string.IsNullOrWhiteSpace(search))
            search = null;
        if (string.IsNullOrWhiteSpace(ownerId))
            ownerId = null;

        var query = new AdminRecipeQuery(SearchText: search, OwnerId: ownerId, Page: page, PageSize: pageSize);
        var result = await _recipeService.GetAllRecipesAsync(query, ct);
        return Ok(result);
    }

    [HttpDelete("recipes/{id:guid}")]
    public async Task<IActionResult> DeleteRecipe(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();

        await _recipeService.DeleteAsync(id, userId, isAdmin: true, ct);
        return NoContent();
    }

    private string RequireUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");
        return userId;
    }

    private IActionResult Problem(int statusCode, string detail)
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
