namespace RecipeBook.Application.DTOs;

public record ImportRecipeResult(string SourceUrl, CreateRecipeRequest Recipe);
