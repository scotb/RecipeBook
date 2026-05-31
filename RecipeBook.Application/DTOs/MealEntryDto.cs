using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.DTOs;

public record MealEntryDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    MealSlot MealSlot,
    int ServingCount,
    MealEntryRecipeDto Recipe
);
