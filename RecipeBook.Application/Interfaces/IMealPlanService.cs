using RecipeBook.Application.DTOs;

namespace RecipeBook.Application.Interfaces;

public interface IMealPlanService
{
    Task<IReadOnlyList<MealPlanSummaryDto>> GetByUserAsync(string userId, CancellationToken ct = default);
    Task<MealPlanDto> GetByIdAsync(Guid id, string requestingUserId, CancellationToken ct = default);
    Task<MealPlanDto> CreateAsync(CreateMealPlanRequest request, string userId, CancellationToken ct = default);
    Task<MealPlanDto> UpdateAsync(Guid id, UpdateMealPlanRequest request, string requestingUserId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, string requestingUserId, CancellationToken ct = default);
    Task<MealEntryDto> SetEntryAsync(Guid mealPlanId, SetMealEntryRequest request, string requestingUserId, CancellationToken ct = default);
    Task ClearEntryAsync(Guid mealPlanId, ClearMealEntryRequest request, string requestingUserId, CancellationToken ct = default);
}
