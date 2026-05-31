using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.DTOs;

public record MealEntryRecipeDto(
    Guid Id,
    string Title,
    string? ImageUrl,
    RecipeCategory Category,
    int ServingSize
);
