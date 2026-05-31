using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.DTOs;

public abstract record RecipePayload
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int? PrepTimeMinutes { get; init; }
    public int? CookTimeMinutes { get; init; }
    public required int ServingSize { get; init; }
    public required RecipeCategory Category { get; init; }
    public RecipeVisibility Visibility { get; init; } = RecipeVisibility.Private;
    public decimal? CaloriesPerServing { get; init; }
    public decimal? ProteinGrams { get; init; }
    public decimal? CarbsGrams { get; init; }
    public decimal? FatGrams { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyList<CreateIngredientRequest>? Ingredients { get; init; }
    public IReadOnlyList<CreateRecipeStepRequest>? Steps { get; init; }
}
