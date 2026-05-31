namespace RecipeBook.Application.DTOs;

public record RecipeStepDto(
    Guid Id,
    int SortOrder,
    string? Title,
    string Body
);
