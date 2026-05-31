using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Application.Interfaces;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.Services;

public sealed class MealPlanService : IMealPlanService
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;

    public MealPlanService(IMealPlanRepository mealPlanRepository, IRecipeRepository recipeRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
    }

    public async Task<IReadOnlyList<MealPlanSummaryDto>> GetByUserAsync(string userId, CancellationToken ct = default)
    {
        var plans = await _mealPlanRepository.GetByUserAsync(userId, ct);
        return plans.Select(ToSummaryDto).ToList();
    }

    public async Task<MealPlanDto> GetByIdAsync(Guid id, string requestingUserId, CancellationToken ct = default)
    {
        var plan = await _mealPlanRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"MealPlan '{id}' not found.");

        if (plan.UserId != requestingUserId)
            throw new ForbiddenException("You do not have access to this meal plan.");

        var recipeMap = await LoadRecipeMapAsync(plan, ct);
        return ToDto(plan, recipeMap);
    }

    public async Task<MealPlanDto> CreateAsync(CreateMealPlanRequest request, string userId, CancellationToken ct = default)
    {
        var plan = new MealPlan(userId, request.WeekStartDate, request.Name);
        await _mealPlanRepository.AddAsync(plan, ct);
        return ToDto(plan, new Dictionary<Guid, Recipe>());
    }

    public async Task<MealPlanDto> UpdateAsync(Guid id, UpdateMealPlanRequest request, string requestingUserId, CancellationToken ct = default)
    {
        var plan = await _mealPlanRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"MealPlan '{id}' not found.");

        if (plan.UserId != requestingUserId)
            throw new ForbiddenException("You do not have permission to update this meal plan.");

        plan.Rename(request.Name);
        await _mealPlanRepository.UpdateAsync(plan, ct);

        var recipeMap = await LoadRecipeMapAsync(plan, ct);
        return ToDto(plan, recipeMap);
    }

    public async Task DeleteAsync(Guid id, string requestingUserId, CancellationToken ct = default)
    {
        var plan = await _mealPlanRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"MealPlan '{id}' not found.");

        if (plan.UserId != requestingUserId)
            throw new ForbiddenException("You do not have permission to delete this meal plan.");

        await _mealPlanRepository.DeleteAsync(id, ct);
    }

    public async Task<MealEntryDto> SetEntryAsync(Guid mealPlanId, SetMealEntryRequest request, string requestingUserId, CancellationToken ct = default)
    {
        var plan = await _mealPlanRepository.GetByIdAsync(mealPlanId, ct)
            ?? throw new NotFoundException($"MealPlan '{mealPlanId}' not found.");

        if (plan.UserId != requestingUserId)
            throw new ForbiddenException("You do not have permission to modify this meal plan.");

        var recipe = await _recipeRepository.GetByIdAsync(request.RecipeId, ct)
            ?? throw new NotFoundException($"Recipe '{request.RecipeId}' not found.");

        var servingCount = request.ServingCount ?? recipe.ServingSize;
        plan.SetEntry(request.DayOfWeek, request.MealSlot, request.RecipeId, servingCount);
        await _mealPlanRepository.UpdateAsync(plan, ct);

        var entry = plan.Entries.First(e => e.DayOfWeek == request.DayOfWeek && e.MealSlot == request.MealSlot);
        var recipeMap = new Dictionary<Guid, Recipe> { [recipe.Id] = recipe };

        return ToEntryDto(entry, recipeMap);
    }

    public async Task ClearEntryAsync(Guid mealPlanId, ClearMealEntryRequest request, string requestingUserId, CancellationToken ct = default)
    {
        var plan = await _mealPlanRepository.GetByIdAsync(mealPlanId, ct)
            ?? throw new NotFoundException($"MealPlan '{mealPlanId}' not found.");

        if (plan.UserId != requestingUserId)
            throw new ForbiddenException("You do not have permission to modify this meal plan.");

        plan.ClearEntry(request.DayOfWeek, request.MealSlot);
        await _mealPlanRepository.UpdateAsync(plan, ct);
    }

    private static MealPlanDto ToDto(MealPlan plan, IReadOnlyDictionary<Guid, Recipe> recipeMap) => new(
        Id: plan.Id,
        Name: plan.Name,
        WeekStartDate: plan.WeekStartDate,
        CreatedAt: plan.CreatedAt,
        Entries: plan.Entries
            .Select(e => ToEntryDto(e, recipeMap))
            .ToList());

    private static MealEntryDto ToEntryDto(MealEntry entry, IReadOnlyDictionary<Guid, Recipe> recipeMap)
    {
        recipeMap.TryGetValue(entry.RecipeId, out var recipe);
        return new MealEntryDto(
            Id: entry.Id,
            DayOfWeek: entry.DayOfWeek,
            MealSlot: entry.MealSlot,
            ServingCount: entry.ServingCount,
            Recipe: recipe is not null
                ? new MealEntryRecipeDto(recipe.Id, recipe.Title, recipe.ImageUrl, recipe.Category, recipe.ServingSize)
                : new MealEntryRecipeDto(entry.RecipeId, "Unknown", null, RecipeCategory.Dinner, 1));
    }

    private static MealPlanSummaryDto ToSummaryDto(MealPlan plan) => new(
        Id: plan.Id,
        Name: plan.Name,
        WeekStartDate: plan.WeekStartDate,
        CreatedAt: plan.CreatedAt,
        EntryCount: plan.Entries.Count);

    private async Task<IReadOnlyDictionary<Guid, Recipe>> LoadRecipeMapAsync(MealPlan plan, CancellationToken ct)
    {
        var uniqueIds = plan.Entries.Select(e => e.RecipeId).Distinct().ToList();
        var recipes = await Task.WhenAll(uniqueIds.Select(id => _recipeRepository.GetByIdAsync(id, ct)));
        return uniqueIds
            .Zip(recipes)
            .Where(pair => pair.Second is not null)
            .ToDictionary(pair => pair.First, pair => pair.Second!);
    }
}
