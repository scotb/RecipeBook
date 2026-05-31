using FluentAssertions;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Domain.Tests.Entities;

public class MealPlanTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesMealPlanWithCorrectDefaults()
    {
        var monday = new DateOnly(2026, 6, 1); // known Monday
        var before = DateTimeOffset.UtcNow;

        var plan = new MealPlan("user-123", monday);

        var after = DateTimeOffset.UtcNow;

        plan.Id.Should().NotBe(Guid.Empty);
        plan.UserId.Should().Be("user-123");
        plan.WeekStartDate.Should().Be(monday);
        plan.Name.Should().BeNull();
        plan.Entries.Should().BeEmpty();
        plan.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceUserId_ThrowsArgumentException(string? userId)
    {
        var monday = new DateOnly(2026, 6, 1);

        var act = () => new MealPlan(userId!, monday);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(2026, 6, 2)]  // Tuesday
    [InlineData(2026, 6, 3)]  // Wednesday
    [InlineData(2026, 6, 7)]  // Sunday
    public void Constructor_WithNonMondayWeekStartDate_ThrowsArgumentException(int year, int month, int day)
    {
        var act = () => new MealPlan("user-123", new DateOnly(year, month, day));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(2026, 6, 1)]   // Monday
    [InlineData(2026, 6, 8)]   // Monday
    [InlineData(2026, 12, 28)] // Monday
    public void Constructor_WithMondayWeekStartDate_Succeeds(int year, int month, int day)
    {
        var act = () => new MealPlan("user-123", new DateOnly(year, month, day));

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNameExceedingMaxLength_ThrowsArgumentException()
    {
        var monday = new DateOnly(2026, 6, 1);
        var longName = new string('x', 101);

        var act = () => new MealPlan("user-123", monday, longName);

        act.Should().Throw<ArgumentException>();
    }

    // ── SetEntry ────────────────────────────────────────────────────────────

    [Fact]
    public void SetEntry_WithNewSlot_AddsEntryToEntries()
    {
        var plan = MakePlan();
        var recipeId = Guid.NewGuid();

        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, recipeId, servingCount: 2);

        plan.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void SetEntry_WithNewSlot_SetsCorrectDaySlotRecipeIdAndServingCount()
    {
        var plan = MakePlan();
        var recipeId = Guid.NewGuid();

        plan.SetEntry(DayOfWeek.Wednesday, MealSlot.Lunch, recipeId, servingCount: 3);

        var entry = plan.Entries.Single();
        entry.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
        entry.MealSlot.Should().Be(MealSlot.Lunch);
        entry.RecipeId.Should().Be(recipeId);
        entry.ServingCount.Should().Be(3);
        entry.MealPlanId.Should().Be(plan.Id);
        entry.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SetEntry_WithNullServingCount_DefaultsServingCountToOne()
    {
        var plan = MakePlan();

        plan.SetEntry(DayOfWeek.Monday, MealSlot.Breakfast, Guid.NewGuid(), servingCount: null);

        plan.Entries.Single().ServingCount.Should().Be(1);
    }

    [Fact]
    public void SetEntry_WhenSlotAlreadyExists_UpdatesExistingEntryInPlace()
    {
        var plan = MakePlan();
        var originalRecipeId = Guid.NewGuid();
        var newRecipeId = Guid.NewGuid();
        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, originalRecipeId, servingCount: 2);

        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, newRecipeId, servingCount: 4);

        var entry = plan.Entries.Single();
        entry.RecipeId.Should().Be(newRecipeId);
        entry.ServingCount.Should().Be(4);
    }

    [Fact]
    public void SetEntry_WhenSlotAlreadyExists_DoesNotAddDuplicateEntry()
    {
        var plan = MakePlan();
        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, Guid.NewGuid(), servingCount: 2);

        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, Guid.NewGuid(), servingCount: 4);

        plan.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void SetEntry_WithNullRecipeId_RemovesExistingEntry()
    {
        var plan = MakePlan();
        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, Guid.NewGuid(), servingCount: 2);

        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, recipeId: null);

        plan.Entries.Should().BeEmpty();
    }

    [Fact]
    public void SetEntry_WithNullRecipeId_WhenSlotIsEmpty_DoesNothing()
    {
        var plan = MakePlan();

        var act = () => plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, recipeId: null);

        act.Should().NotThrow();
        plan.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetEntry_WithServingCountLessThanOne_ThrowsArgumentOutOfRangeException(int servingCount)
    {
        var plan = MakePlan();

        var act = () => plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, Guid.NewGuid(), servingCount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── ClearEntry ───────────────────────────────────────────────────────────

    [Fact]
    public void ClearEntry_WhenSlotExists_RemovesEntry()
    {
        var plan = MakePlan();
        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, Guid.NewGuid(), servingCount: 2);

        plan.ClearEntry(DayOfWeek.Monday, MealSlot.Dinner);

        plan.Entries.Should().BeEmpty();
    }

    [Fact]
    public void ClearEntry_WhenSlotDoesNotExist_DoesNothing()
    {
        var plan = MakePlan();

        var act = () => plan.ClearEntry(DayOfWeek.Monday, MealSlot.Dinner);

        act.Should().NotThrow();
        plan.Entries.Should().BeEmpty();
    }

    // ── multiple entries ─────────────────────────────────────────────────────

    [Fact]
    public void SetEntry_MultipleDistinctSlots_AllEntriesPresent()
    {
        var plan = MakePlan();

        plan.SetEntry(DayOfWeek.Monday, MealSlot.Breakfast, Guid.NewGuid());
        plan.SetEntry(DayOfWeek.Tuesday, MealSlot.Lunch, Guid.NewGuid());
        plan.SetEntry(DayOfWeek.Friday, MealSlot.Dinner, Guid.NewGuid());

        plan.Entries.Should().HaveCount(3);
    }

    [Fact]
    public void SetEntry_SameDayDifferentSlots_AreIndependent()
    {
        var plan = MakePlan();
        var breakfastId = Guid.NewGuid();
        var dinnerId = Guid.NewGuid();

        plan.SetEntry(DayOfWeek.Monday, MealSlot.Breakfast, breakfastId);
        plan.SetEntry(DayOfWeek.Monday, MealSlot.Dinner, dinnerId);

        plan.Entries.Should().HaveCount(2);
        plan.Entries.Single(e => e.MealSlot == MealSlot.Breakfast).RecipeId.Should().Be(breakfastId);
        plan.Entries.Single(e => e.MealSlot == MealSlot.Dinner).RecipeId.Should().Be(dinnerId);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static MealPlan MakePlan() => new("user-123", new DateOnly(2026, 6, 1));
}
