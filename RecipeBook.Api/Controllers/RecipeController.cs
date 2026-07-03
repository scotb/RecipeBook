
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;

namespace RecipeBook.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/recipes")]
public class RecipeController : BaseController
{
    private readonly IRecipeService _recipeService;
    private readonly IUserContext _userContext;

    public RecipeController(IRecipeService recipeService, IUserContext userContext)
    {
        _recipeService = recipeService;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicRecipes(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? tags = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (error, query) = RecipeQueryBuilder.Create(search, category, tags, page, pageSize);
        if (error is not null)
            return Problem(400, error);

        var result = await _recipeService.GetPublicRecipesAsync(query!, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRecipe(Guid id, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.GetByIdAsync(id, _userContext.UserId, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipe(
        Application.DTOs.CreateRecipeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.CreateAsync(request, _userContext.UserId, ct);
        return CreatedAtAction(nameof(GetRecipe), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRecipe(
        Guid id, Application.DTOs.UpdateRecipeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.UpdateAsync(id, request, _userContext.UserId, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRecipe(Guid id, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");
        await _recipeService.DeleteAsync(id, _userContext.UserId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/fork")]
    public async Task<IActionResult> ForkRecipe(Guid id, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.ForkAsync(id, _userContext.UserId, ct);
        return CreatedAtAction(nameof(GetRecipe), new { id = result.Id }, result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportRecipe(
        Application.DTOs.ImportRecipeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.ImportAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRecipes(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? tags = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
            throw new InvalidOperationException("User identity claim is missing.");

        var (error, query) = RecipeQueryBuilder.Create(search, category, tags, page, pageSize);
        if (error is not null)
            return Problem(400, error);

        var result = await _recipeService.GetMyRecipesAsync(_userContext.UserId, query!, ct);
        return Ok(result);
    }



}
