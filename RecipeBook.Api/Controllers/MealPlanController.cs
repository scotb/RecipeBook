using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;

namespace RecipeBook.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/mealplans")]
public class MealPlanController : ControllerBase
{
    private readonly IMealPlanService _mealPlanService;

    public MealPlanController(IMealPlanService mealPlanService)
    {
        _mealPlanService = mealPlanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMealPlans(CancellationToken ct = default)
    {
        var userId = GetUserId();
        var plans = await _mealPlanService.GetByUserAsync(userId, ct);
        return Ok(new PagedResult<MealPlanSummaryDto>(plans, plans.Count, 1, plans.Count));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMealPlan(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var result = await _mealPlanService.GetByIdAsync(id, userId, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMealPlan(
        CreateMealPlanRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Problem(400, "Invalid request body.");

        var userId = GetUserId();
        var result = await _mealPlanService.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetMealPlan), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMealPlan(
        Guid id, UpdateMealPlanRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Problem(400, "Invalid request body.");

        var userId = GetUserId();
        var result = await _mealPlanService.UpdateAsync(id, request, userId, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMealPlan(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        await _mealPlanService.DeleteAsync(id, userId, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/entries")]
    public async Task<IActionResult> SetMealPlanEntry(
        Guid id, SetMealEntryRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Problem(400, "Invalid request body.");

        var userId = GetUserId();
        var result = await _mealPlanService.SetEntryAsync(id, request, userId, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/entries")]
    public async Task<IActionResult> ClearMealPlanEntry(
        Guid id, ClearMealEntryRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Problem(400, "Invalid request body.");

        var userId = GetUserId();
        await _mealPlanService.ClearEntryAsync(id, request, userId, ct);
        return NoContent();
    }

    private string GetUserId()
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
