using RecipeBook.Application.Models;
using RecipeBook.Domain.Entities;

namespace RecipeBook.Application.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Recipe>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<PagedResult<Recipe>> GetPublicAsync(RecipeQuery query, CancellationToken ct = default);
    Task<PagedResult<Recipe>> GetByOwnerAsync(string ownerId, RecipeQuery query, CancellationToken ct = default);
    Task<PagedResult<Recipe>> GetAllAsync(AdminRecipeQuery query, CancellationToken ct = default);
    Task<Recipe> AddAsync(Recipe recipe, CancellationToken ct = default);
    Task UpdateAsync(Recipe recipe, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasForkAsync(Guid sourceRecipeId, string ownerId, CancellationToken ct = default);
}
