using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.DTOs;

public record SetMealEntryRequest(
    DayOfWeek DayOfWeek,
    MealSlot MealSlot,
    Guid RecipeId,
    int? ServingCount
);
