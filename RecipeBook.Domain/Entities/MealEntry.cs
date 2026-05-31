using RecipeBook.Domain.Enums;

namespace RecipeBook.Domain.Entities;

public sealed class MealEntry
{
    internal MealEntry(Guid mealPlanId, DayOfWeek dayOfWeek, MealSlot mealSlot, Guid recipeId, int servingCount)
    {
        Id = Guid.NewGuid();
        MealPlanId = mealPlanId;
        DayOfWeek = dayOfWeek;
        MealSlot = mealSlot;
        RecipeId = recipeId;
        ServingCount = servingCount;
    }

    public Guid Id { get; }
    public Guid MealPlanId { get; }
    public DayOfWeek DayOfWeek { get; }
    public MealSlot MealSlot { get; }
    public Guid RecipeId { get; internal set; }
    public int ServingCount { get; internal set; }
}
