using RecipeBook.Application.DTOs;

namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// Thin API wrapper for the Recipe endpoints.
/// </summary>
public interface IRecipeApiService
{
    Task<PagedResult<RecipeSummaryDto>> GetPublicRecipesAsync(
        string? search, string? category, string? tags, int page, int pageSize, CancellationToken ct = default);

    Task<PagedResult<RecipeSummaryDto>> GetMyRecipesAsync(
        string? search, string? category, string? tags, int page, int pageSize, CancellationToken ct = default);

    Task<RecipeDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RecipeDto> CreateAsync(CreateRecipeRequest request, CancellationToken ct = default);
    Task<RecipeDto> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<RecipeDto> ForkAsync(Guid id, CancellationToken ct = default);
    Task<ImportRecipeRequest> ImportAsync(string url, CancellationToken ct = default);
}
