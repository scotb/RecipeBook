using Microsoft.EntityFrameworkCore;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;
using RecipeBook.Infrastructure.Persistence;

namespace RecipeBook.Infrastructure.Repositories;

public sealed class RecipeRepository : IRecipeRepository
{
    private readonly RecipeBookDbContext _context;

    public RecipeRepository(RecipeBookDbContext context) => _context = context;

    public async Task<Recipe> AddAsync(Recipe recipe, CancellationToken ct = default)
    {
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync(ct);
        return recipe;
    }

    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Recipe>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];
        return await _context.Recipes
            .Where(r => idList.Contains(r.Id))
            .ToListAsync(ct);
    }

    public Task<PagedResult<Recipe>> GetPublicAsync(RecipeQuery query, CancellationToken ct = default)
    {
        var q = _context.Recipes.Where(r => r.Visibility == RecipeVisibility.Public);
        return ToPagedResultAsync(ApplyQueryFilters(q, query), query.Page, query.PageSize, ct);
    }

    public Task<PagedResult<Recipe>> GetByOwnerAsync(string ownerId, RecipeQuery query, CancellationToken ct = default)
    {
        var q = _context.Recipes.Where(r => r.OwnerId == ownerId);
        return ToPagedResultAsync(ApplyQueryFilters(q, query), query.Page, query.PageSize, ct);
    }

    public async Task<PagedResult<Recipe>> GetAllAsync(AdminRecipeQuery query, CancellationToken ct = default)
    {
        IQueryable<Recipe> q = _context.Recipes;

        if (!string.IsNullOrWhiteSpace(query.OwnerId))
            q = q.Where(r => r.OwnerId == query.OwnerId);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var pattern = $"%{query.SearchText}%";
            q = q.Where(r => EF.Functions.ILike(r.Title, pattern) ||
                              (r.Description != null && EF.Functions.ILike(r.Description, pattern)));
        }

        return await ToPagedResultAsync(q, query.Page, query.PageSize, ct);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken ct = default)
    {
        _context.Recipes.Update(recipe);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var recipe = await _context.Recipes.FindAsync([id], ct);
        if (recipe is not null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync(ct);
        }
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _context.Recipes.AnyAsync(r => r.Id == id, ct);

    private static IQueryable<Recipe> ApplyQueryFilters(IQueryable<Recipe> q, RecipeQuery query)
    {
        if (query.Category.HasValue)
            q = q.Where(r => r.Category == query.Category.Value);

        if (query.Tags is { Count: > 0 })
            foreach (var tag in query.Tags)
                q = q.Where(r => r.Tags.Any(t => t.Name == tag));

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var pattern = $"%{query.SearchText}%";
            q = q.Where(r => EF.Functions.ILike(r.Title, pattern) ||
                              (r.Description != null && EF.Functions.ILike(r.Description, pattern)));
        }

        return q;
    }

    private static async Task<PagedResult<Recipe>> ToPagedResultAsync(
        IQueryable<Recipe> q, int page, int pageSize, CancellationToken ct)
    {
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new PagedResult<Recipe>(items, total, page, pageSize);
    }
}

