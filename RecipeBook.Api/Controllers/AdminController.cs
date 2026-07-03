using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;

namespace RecipeBook.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminController : BaseController
{
    private readonly IRecipeService _recipeService;
    private readonly IUserContext _userContext;

    public AdminController(IRecipeService recipeService, IUserContext userContext)
    {
        _recipeService = recipeService;
        _userContext = userContext;
    }

    [HttpGet("recipes")]
    public async Task<IActionResult> GetRecipes(
        [FromQuery] string? search = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");

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
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");

        await _recipeService.DeleteAsync(id, _userContext.UserId, ct);
        return NoContent();
    }

}
