using RecipeBook.Domain.Entities;

namespace RecipeBook.Application.Interfaces;

public interface IMealPlanRepository
{
    Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MealPlan>> GetByUserAsync(string userId, CancellationToken ct = default);
    Task<MealPlan> AddAsync(MealPlan mealPlan, CancellationToken ct = default);
    Task UpdateAsync(MealPlan mealPlan, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
