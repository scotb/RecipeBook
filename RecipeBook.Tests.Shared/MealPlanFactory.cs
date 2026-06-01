using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Tests.Shared;

public static class MealPlanFactory
{
    public static MealPlan CreateValid(
        string userId = "test-user",
        DateOnly? weekStartDate = null)
    {
        var startDate = weekStartDate ?? GetNextMonday();
        return new MealPlan(userId, startDate);
    }

    public static MealPlan CreateWithEntry(
        Guid recipeId,
        string userId = "test-user",
        DayOfWeek day = DayOfWeek.Monday,
        MealSlot slot = MealSlot.Dinner,
        int servingCount = 2)
    {
        var plan = CreateValid(userId);
        plan.SetEntry(day, slot, recipeId, servingCount);
        return plan;
    }

    private static DateOnly GetNextMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        return daysUntilMonday == 0 ? today : today.AddDays(daysUntilMonday);
    }
}
