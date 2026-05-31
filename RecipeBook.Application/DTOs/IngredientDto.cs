namespace RecipeBook.Application.DTOs;

public record IngredientDto(
    Guid Id,
    int SortOrder,
    decimal? Quantity,
    string? Unit,
    string Name,
    string? Notes
);
