using FluentAssertions;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Services;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.Tests.Services;

public class MealPlanServiceTests
{
    private readonly Mock<IMealPlanRepository> _mealPlanRepoMock = new();
    private readonly Mock<IRecipeRepository> _recipeRepoMock = new();
    private readonly MealPlanService _sut;

    // Next Monday from a fixed base date for determinism
    private static readonly DateOnly NextMonday = new DateOnly(2026, 6, 1); // a known Monday

    public MealPlanServiceTests()
    {
        _sut = new MealPlanService(_mealPlanRepoMock.Object, _recipeRepoMock.Object);

        // Default: batch fetch returns empty list (tests with entries override this)
        _recipeRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsMealPlanDto()
    {
        var request = new CreateMealPlanRequest(NextMonday, "Week 1");

        _mealPlanRepoMock
            .Setup(r => r.AddAsync(It.IsAny<MealPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MealPlan p, CancellationToken _) => p);

        var result = await _sut.CreateAsync(request, "user-1");

        result.Name.Should().Be("Week 1");
        result.WeekStartDate.Should().Be(NextMonday);
        result.Entries.Should().BeEmpty();
        _mealPlanRepoMock.Verify(r => r.AddAsync(It.IsAny<MealPlan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenOwner_ReturnsDto()
    {
        var plan = new MealPlan("user-1", NextMonday, "My Plan");
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var result = await _sut.GetByIdAsync(plan.Id, requestingUserId: "user-1");

        result.Id.Should().Be(plan.Id);
        result.Name.Should().Be("My Plan");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var plan = new MealPlan("user-1", NextMonday);
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var act = () => _sut.GetByIdAsync(plan.Id, requestingUserId: "other-user");

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MealPlan?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid(), "user-1");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetByUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_ReturnsSummaries()
    {
        var plan1 = new MealPlan("user-1", NextMonday, "Week A");
        var plan2 = new MealPlan("user-1", NextMonday.AddDays(7), "Week B");

        _mealPlanRepoMock.Setup(r => r.GetByUserAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MealPlan> { plan1, plan2 });

        var result = await _sut.GetByUserAsync("user-1");

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Week A");
        result[1].Name.Should().Be("Week B");
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenOwner_UpdatesName()
    {
        var plan = new MealPlan("user-1", NextMonday, "Old Name");
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var result = await _sut.UpdateAsync(plan.Id, new UpdateMealPlanRequest("New Name"), requestingUserId: "user-1");

        result.Name.Should().Be("New Name");
        _mealPlanRepoMock.Verify(r => r.UpdateAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var plan = new MealPlan("user-1", NextMonday);
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var act = () => _sut.UpdateAsync(plan.Id, new UpdateMealPlanRequest("X"), requestingUserId: "other");

        await act.Should().ThrowAsync<ForbiddenException>();
        _mealPlanRepoMock.Verify(r => r.UpdateAsync(It.IsAny<MealPlan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenOwner_DeletesMealPlan()
    {
        var plan = new MealPlan("user-1", NextMonday);
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        await _sut.DeleteAsync(plan.Id, requestingUserId: "user-1");

        _mealPlanRepoMock.Verify(r => r.DeleteAsync(plan.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var plan = new MealPlan("user-1", NextMonday);
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var act = () => _sut.DeleteAsync(plan.Id, requestingUserId: "other");

        await act.Should().ThrowAsync<ForbiddenException>();
        _mealPlanRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── SetEntryAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetEntryAsync_WhenOwner_AddsEntryAndReturnsDto()
    {
        var plan = new MealPlan("user-1", NextMonday);
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.Public);
        var request = new SetMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner, recipe.Id, 2);

        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _recipeRepoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var result = await _sut.SetEntryAsync(plan.Id, request, requestingUserId: "user-1");

        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
        result.MealSlot.Should().Be(MealSlot.Dinner);
        result.ServingCount.Should().Be(2);
        result.Recipe.Title.Should().Be("Pasta");
        _mealPlanRepoMock.Verify(r => r.UpdateAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetEntryAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var plan = new MealPlan("user-1", NextMonday);
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var request = new SetMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner, Guid.NewGuid(), 1);

        var act = () => _sut.SetEntryAsync(plan.Id, request, requestingUserId: "other");

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── ClearEntryAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ClearEntryAsync_WhenOwner_RemovesEntry()
    {
        var recipeId = Guid.NewGuid();
        var plan = new MealPlan("user-1", NextMonday);
        plan.SetEntry(DayOfWeek.Tuesday, MealSlot.Lunch, recipeId, 1);

        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var request = new ClearMealEntryRequest(DayOfWeek.Tuesday, MealSlot.Lunch);

        await _sut.ClearEntryAsync(plan.Id, request, requestingUserId: "user-1");

        plan.Entries.Should().BeEmpty();
        _mealPlanRepoMock.Verify(r => r.UpdateAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearEntryAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var plan = new MealPlan("user-1", NextMonday);
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var act = () => _sut.ClearEntryAsync(plan.Id, new ClearMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner), requestingUserId: "other");

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── SetEntryAsync (additional behaviors) ─────────────────────────────────

    [Fact]
    public async Task SetEntryAsync_WhenRecipeNotFound_ThrowsNotFoundException()
    {
        var plan = new MealPlan("user-1", NextMonday);
        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Recipe?)null);

        var request = new SetMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner, Guid.NewGuid(), ServingCount: 2);

        var act = () => _sut.SetEntryAsync(plan.Id, request, requestingUserId: "user-1");

        await act.Should().ThrowAsync<NotFoundException>();
        _mealPlanRepoMock.Verify(r => r.UpdateAsync(It.IsAny<MealPlan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetEntryAsync_WhenServingCountNull_DefaultsToRecipeServingSize()
    {
        var plan = new MealPlan("user-1", NextMonday);
        var recipe = new Recipe("Tacos", "owner-1", 4, RecipeCategory.Dinner, visibility: RecipeVisibility.Public);

        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _recipeRepoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var request = new SetMealEntryRequest(DayOfWeek.Wednesday, MealSlot.Dinner, recipe.Id, ServingCount: null);

        var result = await _sut.SetEntryAsync(plan.Id, request, requestingUserId: "user-1");

        result.ServingCount.Should().Be(4); // defaulted to recipe.ServingSize
    }

    // ── SetEntryAsync (recipe access control) ────────────────────────────────

    [Fact]
    public async Task SetEntryAsync_WhenRecipeIsPrivateAndOwnedByRequestingUser_Succeeds()
    {
        var plan = new MealPlan("user-1", NextMonday);
        var recipe = new Recipe("My Private Recipe", "user-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.Private);

        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _recipeRepoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var request = new SetMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner, recipe.Id, ServingCount: 2);

        var act = () => _sut.SetEntryAsync(plan.Id, request, requestingUserId: "user-1");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetEntryAsync_WhenRecipeIsPrivateAndNotOwnedByRequestingUser_ThrowsForbiddenException()
    {
        var plan = new MealPlan("user-1", NextMonday);
        var recipe = new Recipe("Someone Else's Recipe", "owner-2", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.Private);

        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _recipeRepoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var request = new SetMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner, recipe.Id, ServingCount: 2);

        var act = () => _sut.SetEntryAsync(plan.Id, request, requestingUserId: "user-1");

        await act.Should().ThrowAsync<ForbiddenException>();
        _mealPlanRepoMock.Verify(r => r.UpdateAsync(It.IsAny<MealPlan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── LoadRecipeMapAsync (batch fetching) ───────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenPlanHasMultipleEntries_UsesBatchFetch()
    {
        var recipe1 = new Recipe("Pasta", "owner-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.Public);
        var recipe2 = new Recipe("Salad", "owner-1", 1, RecipeCategory.Lunch, visibility: RecipeVisibility.Public);
        var plan = new MealPlan("user-1", NextMonday);
        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, recipe1.Id, 2);
        plan.SetEntry(DayOfWeek.Tuesday, MealSlot.Lunch, recipe2.Id, 1);

        _mealPlanRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _recipeRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([recipe1, recipe2]);

        await _sut.GetByIdAsync(plan.Id, requestingUserId: "user-1");

        _recipeRepoMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        _recipeRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
