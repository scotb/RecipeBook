namespace RecipeBook.Application.DTOs;

public record CreateRecipeStepRequest(
    string? Title,
    string Body
);
