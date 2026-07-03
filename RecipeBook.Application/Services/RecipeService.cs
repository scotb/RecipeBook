using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.Services;

public sealed class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IUserLookupService _userLookup;
    private readonly IRecipeImporter _recipeImporter;
    private readonly IUserContext _userContext;

    public RecipeService(
        IRecipeRepository recipeRepository,
        IUserLookupService userLookup,
        IRecipeImporter recipeImporter,
        IUserContext userContext)
    {
        _recipeRepository = recipeRepository;
        _userLookup = userLookup;
        _recipeImporter = recipeImporter;
        _userContext = userContext;
    }

    public async Task<PagedResult<RecipeSummaryDto>> GetPublicRecipesAsync(RecipeQuery query, CancellationToken ct = default)
    {
        var paged = await _recipeRepository.GetPublicAsync(query, ct);
        return await ToSummaryPageAsync(paged, ct);
    }

    public async Task<PagedResult<RecipeSummaryDto>> GetMyRecipesAsync(string ownerId, RecipeQuery query, CancellationToken ct = default)
    {
        var paged = await _recipeRepository.GetByOwnerAsync(ownerId, query, ct);
        return await ToSummaryPageAsync(paged, ct);
    }

    public async Task<PagedResult<RecipeSummaryDto>> GetAllRecipesAsync(AdminRecipeQuery query, CancellationToken ct = default)
    {
        var paged = await _recipeRepository.GetAllAsync(query, ct);
        return await ToSummaryPageAsync(paged, ct);
    }

    public async Task<RecipeDto> GetByIdAsync(Guid id, string requestingUserId, CancellationToken ct = default)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Recipe '{id}' not found.");

        if (recipe.Visibility != RecipeVisibility.Public
            && recipe.OwnerId != requestingUserId
            && !_userContext.IsInRole("Admin"))
            throw new ForbiddenException("You do not have access to this recipe.");

        var displayName = await _userLookup.GetDisplayNameAsync(recipe.OwnerId, ct) ?? recipe.OwnerId;
        return ToDto(recipe, displayName);
    }

    public async Task<RecipeDto> CreateAsync(CreateRecipeRequest request, string ownerId, CancellationToken ct = default)
    {
        var recipe = new Recipe(
            title: request.Title,
            ownerId: ownerId,
            servingSize: request.ServingSize,
            category: request.Category,
            description: request.Description,
            imageUrl: request.ImageUrl,
            prepTimeMinutes: request.PrepTimeMinutes,
            cookTimeMinutes: request.CookTimeMinutes,
            visibility: request.Visibility ?? RecipeVisibility.Private,
            caloriesPerServing: request.CaloriesPerServing,
            proteinGrams: request.ProteinGrams,
            carbsGrams: request.CarbsGrams,
            fatGrams: request.FatGrams);

        if (request.Tags is not null)
            foreach (var tag in request.Tags)
                recipe.AddTag(tag);

        if (request.Ingredients is not null)
            foreach (var ing in request.Ingredients)
                recipe.AddIngredient(ing.Name, ing.Quantity, ing.Unit, ing.Notes);

        if (request.Steps is not null)
            foreach (var step in request.Steps)
                recipe.AddStep(step.Body, step.Title);

        await _recipeRepository.AddAsync(recipe, ct);

        var displayName = await _userLookup.GetDisplayNameAsync(ownerId, ct) ?? ownerId;
        return ToDto(recipe, displayName);
    }

    public async Task<RecipeDto> UpdateAsync(Guid id, UpdateRecipeRequest request, string requestingUserId, CancellationToken ct = default)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Recipe '{id}' not found.");

        if (recipe.OwnerId != requestingUserId && !_userContext.IsInRole("Admin"))
            throw new ForbiddenException("You do not have permission to update this recipe.");

        recipe.Update(
            title: request.Title,
            servingSize: request.ServingSize,
            category: request.Category,
            description: request.Description,
            imageUrl: request.ImageUrl,
            prepTimeMinutes: request.PrepTimeMinutes,
            cookTimeMinutes: request.CookTimeMinutes,
            caloriesPerServing: request.CaloriesPerServing,
            proteinGrams: request.ProteinGrams,
            carbsGrams: request.CarbsGrams,
            fatGrams: request.FatGrams);

        if (request.Tags is not null)
        {
            recipe.ClearTags();
            foreach (var tag in request.Tags)
                recipe.AddTag(tag);
        }

        if (request.Ingredients is not null)
        {
            recipe.ClearIngredients();
            foreach (var ing in request.Ingredients)
                recipe.AddIngredient(ing.Name, ing.Quantity, ing.Unit, ing.Notes);
        }

        if (request.Steps is not null)
        {
            recipe.ClearSteps();
            foreach (var step in request.Steps)
                recipe.AddStep(step.Body, step.Title);
        }

        if (request.Visibility is not null)
        {
            if (request.Visibility == RecipeVisibility.Private)
                recipe.MakePrivate();
            else if (request.Visibility == RecipeVisibility.PendingReview)
                recipe.SubmitForReview();
            else if (request.Visibility == RecipeVisibility.Public && _userContext.IsInRole("Admin"))
                recipe.Approve();
            else if (request.Visibility == RecipeVisibility.Public)
                throw new ForbiddenException("Only admins can publish a recipe directly.");
        }

        await _recipeRepository.UpdateAsync(recipe, ct);

        var displayName = await _userLookup.GetDisplayNameAsync(recipe.OwnerId, ct) ?? recipe.OwnerId;
        return ToDto(recipe, displayName);
    }

    public async Task DeleteAsync(Guid id, string requestingUserId, CancellationToken ct = default)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Recipe '{id}' not found.");

        if (recipe.OwnerId != requestingUserId && !_userContext.IsInRole("Admin"))
            throw new ForbiddenException("You do not have permission to delete this recipe.");

        await _recipeRepository.DeleteAsync(id, ct);
    }

    public async Task<RecipeDto> ForkAsync(Guid sourceRecipeId, string newOwnerId, CancellationToken ct = default)
    {
        var source = await _recipeRepository.GetByIdAsync(sourceRecipeId, ct)
            ?? throw new NotFoundException($"Recipe '{sourceRecipeId}' not found.");

        if (source.Visibility != RecipeVisibility.Public)
            throw new ForbiddenException("You do not have permission to fork this recipe.");

        if (await _recipeRepository.HasForkAsync(sourceRecipeId, newOwnerId, ct))
            throw new ConflictException("You have already forked this recipe.");

        var fork = source.Fork(newOwnerId);
        await _recipeRepository.AddAsync(fork, ct);

        var displayName = await _userLookup.GetDisplayNameAsync(newOwnerId, ct) ?? newOwnerId;
        return ToDto(fork, displayName);
    }

    public async Task<ImportRecipeResult> ImportAsync(ImportRecipeRequest request, CancellationToken ct = default)
    {
        var recipe = await _recipeImporter.ImportFromUrlAsync(request.Url, ct);
        if (recipe.Visibility == RecipeVisibility.Private)
            throw new ForbiddenException("You do not have permission to import this recipe.");
        return new ImportRecipeResult(request.Url, recipe);
    }

    private static RecipeDto ToDto(Recipe recipe, string ownerDisplayName) => new(
        Id: recipe.Id,
        Title: recipe.Title,
        Description: recipe.Description,
        ImageUrl: recipe.ImageUrl,
        PrepTimeMinutes: recipe.PrepTimeMinutes,
        CookTimeMinutes: recipe.CookTimeMinutes,
        ServingSize: recipe.ServingSize,
        Category: recipe.Category,
        Visibility: recipe.Visibility,
        OwnerId: recipe.OwnerId,
        OwnerDisplayName: ownerDisplayName,
        SourceRecipeId: recipe.SourceRecipeId,
        CaloriesPerServing: recipe.CaloriesPerServing,
        ProteinGrams: recipe.ProteinGrams,
        CarbsGrams: recipe.CarbsGrams,
        FatGrams: recipe.FatGrams,
        Tags: recipe.Tags.Select(t => t.Name).ToList(),
        Ingredients: recipe.Ingredients.Select(i => new IngredientDto(i.Id, i.SortOrder, i.Quantity, i.Unit, i.Name, i.Notes)).ToList(),
        Steps: recipe.Steps.Select(s => new RecipeStepDto(s.Id, s.SortOrder, s.Title, s.Body)).ToList(),
        CreatedAt: recipe.CreatedAt,
        UpdatedAt: recipe.UpdatedAt);

    private static RecipeSummaryDto ToSummaryDto(Recipe recipe, string ownerDisplayName) => new(
        Id: recipe.Id,
        Title: recipe.Title,
        Description: recipe.Description,
        ImageUrl: recipe.ImageUrl,
        Category: recipe.Category,
        Visibility: recipe.Visibility,
        ServingSize: recipe.ServingSize,
        PrepTimeMinutes: recipe.PrepTimeMinutes,
        CookTimeMinutes: recipe.CookTimeMinutes,
        Tags: recipe.Tags.Select(t => t.Name).ToList(),
        OwnerId: recipe.OwnerId,
        OwnerDisplayName: ownerDisplayName,
        CreatedAt: recipe.CreatedAt);

    private async Task<PagedResult<RecipeSummaryDto>> ToSummaryPageAsync(PagedResult<Recipe> paged, CancellationToken ct)
    {
        var ownerIds = paged.Items.Select(r => r.OwnerId).Distinct();
        var names = await _userLookup.GetDisplayNamesAsync(ownerIds, ct);

        var items = paged.Items
            .Select(r => ToSummaryDto(r, names.TryGetValue(r.OwnerId, out var n) ? n : r.OwnerId))
            .ToList();

        return new PagedResult<RecipeSummaryDto>(items, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
