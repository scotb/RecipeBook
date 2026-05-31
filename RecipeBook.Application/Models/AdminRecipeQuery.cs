namespace RecipeBook.Application.Models;

public record AdminRecipeQuery(
    string? SearchText = null,
    string? OwnerId = null,
    int Page = 1,
    int PageSize = 20
);
