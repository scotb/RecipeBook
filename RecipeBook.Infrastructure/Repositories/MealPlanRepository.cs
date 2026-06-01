using Microsoft.EntityFrameworkCore;
using RecipeBook.Application.Interfaces;
using RecipeBook.Domain.Entities;
using RecipeBook.Infrastructure.Persistence;

namespace RecipeBook.Infrastructure.Repositories;

public sealed class MealPlanRepository : IMealPlanRepository
{
    private readonly RecipeBookDbContext _context;

    public MealPlanRepository(RecipeBookDbContext context) => _context = context;

    public async Task<MealPlan> AddAsync(MealPlan mealPlan, CancellationToken ct = default)
    {
        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync(ct);
        return mealPlan;
    }

    public async Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.MealPlans
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task<IReadOnlyList<MealPlan>> GetByUserAsync(string userId, CancellationToken ct = default)
        => await _context.MealPlans
            .Where(p => p.UserId == userId)
            .Include(p => p.Entries)
            .OrderByDescending(p => p.WeekStartDate)
            .ToListAsync(ct);
    public async Task UpdateAsync(MealPlan mealPlan, CancellationToken ct = default)
    {
        _context.MealPlans.Update(mealPlan);
        await _context.SaveChangesAsync(ct);
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var mealPlan = await _context.MealPlans.FindAsync([id], ct);
        if (mealPlan is not null)
        {
            _context.MealPlans.Remove(mealPlan);
            await _context.SaveChangesAsync(ct);
        }
    }
}
