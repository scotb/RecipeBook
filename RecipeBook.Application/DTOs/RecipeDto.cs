using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.DTOs;

public record RecipeDto(
    Guid Id,
    string Title,
    string? Description,
    string? ImageUrl,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    int ServingSize,
    RecipeCategory Category,
    RecipeVisibility Visibility,
    string OwnerId,
    string OwnerDisplayName,
    Guid? SourceRecipeId,
    decimal? CaloriesPerServing,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    IReadOnlyList<string> Tags,
    IReadOnlyList<IngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
