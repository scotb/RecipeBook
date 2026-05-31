namespace RecipeBook.Application.DTOs;

public record CreateIngredientRequest(
    decimal? Quantity,
    string? Unit,
    string Name,
    string? Notes
);
