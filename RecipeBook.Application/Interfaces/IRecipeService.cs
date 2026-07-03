using RecipeBook.Application.DTOs;
using RecipeBook.Application.Models;

namespace RecipeBook.Application.Interfaces;

public interface IRecipeService
{
    Task<PagedResult<RecipeSummaryDto>> GetPublicRecipesAsync(RecipeQuery query, CancellationToken ct = default);
    Task<PagedResult<RecipeSummaryDto>> GetMyRecipesAsync(string ownerId, RecipeQuery query, CancellationToken ct = default);
    Task<PagedResult<RecipeSummaryDto>> GetAllRecipesAsync(AdminRecipeQuery query, CancellationToken ct = default);
    Task<RecipeDto> GetByIdAsync(Guid id, string requestingUserId, CancellationToken ct = default);
    Task<RecipeDto> CreateAsync(CreateRecipeRequest request, string ownerId, CancellationToken ct = default);
    Task<RecipeDto> UpdateAsync(Guid id, UpdateRecipeRequest request, string requestingUserId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, string requestingUserId, CancellationToken ct = default);
    Task<RecipeDto> ForkAsync(Guid sourceRecipeId, string newOwnerId, CancellationToken ct = default);
    Task<ImportRecipeResult> ImportAsync(ImportRecipeRequest request, CancellationToken ct = default);
}
