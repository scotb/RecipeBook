using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.DTOs;

public record ClearMealEntryRequest(
    DayOfWeek DayOfWeek,
    MealSlot MealSlot
);
