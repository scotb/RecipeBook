using RecipeBook.Application.DTOs;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// Thin API wrapper for the Meal Plan endpoints.
/// </summary>
public interface IMealPlanApiService
{
    Task<IEnumerable<MealPlanSummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<MealPlanDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MealPlanDto> CreateAsync(CreateMealPlanRequest request, CancellationToken ct = default);
    Task<MealPlanDto> UpdateAsync(Guid id, UpdateMealPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<MealEntryDto> SetEntryAsync(Guid id, SetMealEntryRequest request, CancellationToken ct = default);
    Task DeleteEntryAsync(Guid id, ClearMealEntryRequest request, CancellationToken ct = default);
}
