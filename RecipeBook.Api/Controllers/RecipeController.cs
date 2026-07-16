using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;

namespace RecipeBook.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/recipes")]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipeController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
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
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized();

        var (error, query) = BuildQuery(search, category, tags, page, pageSize);
        if (error is not null)
            return error;

        var result = await _recipeService.GetPublicRecipesAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRecipe(Guid id, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.GetByIdAsync(id, userId, isAdmin: false, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipe(
        Application.DTOs.CreateRecipeRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Problem(400, "Invalid request body.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetRecipe), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRecipe(
        Guid id, Application.DTOs.UpdateRecipeRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Problem(400, "Invalid request body.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");
        var isAdmin = User.IsInRole("Admin");
        var result = await _recipeService.UpdateAsync(id, request, userId, isAdmin, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRecipe(Guid id, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");
        var isAdmin = User.IsInRole("Admin");
        await _recipeService.DeleteAsync(id, userId, isAdmin, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/fork")]
    public async Task<IActionResult> ForkRecipe(Guid id, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");
        var result = await _recipeService.ForkAsync(id, userId, ct);
        return CreatedAtAction(nameof(GetRecipe), new { id = result.Id }, result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportRecipe(
        Application.DTOs.ImportRecipeRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Problem(400, "Invalid request body.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
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
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity claim is missing.");

        var (error, query) = BuildQuery(search, category, tags, page, pageSize);
        if (error is not null)
            return error;

        var result = await _recipeService.GetMyRecipesAsync(userId, query, ct);
        return Ok(result);
    }

    private (IActionResult? Error, RecipeQuery Query) BuildQuery(
        string? search, string? category, string? tags, int page, int pageSize)
    {
        if (!ModelState.IsValid)
            return (Problem(400, "Invalid request parameters."), default!);

        if (page < 1 || pageSize < 1 || pageSize > 100)
            return (Problem(400, "Page and pageSize must be >= 1, pageSize must be <= 100."), default!);

        if (string.IsNullOrWhiteSpace(search))
            search = null;

        Domain.Enums.RecipeCategory? parsedCategory = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<Domain.Enums.RecipeCategory>(category, ignoreCase: true, out var parsedCategoryValue))
                return (Problem(400, $"Invalid category: '{category}'. Valid values: {string.Join(", ", Enum.GetNames<Domain.Enums.RecipeCategory>())}"), default!);
            parsedCategory = parsedCategoryValue;
        }

        IReadOnlyList<string>? parsedTags = null;
        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .ToList();
            if (tagList.Count > 0)
                parsedTags = tagList;
        }

        var query = new RecipeQuery(SearchText: search, Category: parsedCategory, Tags: parsedTags, Page: page, PageSize: pageSize);
        return (null, query);
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
