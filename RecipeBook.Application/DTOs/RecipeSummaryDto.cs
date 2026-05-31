using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.DTOs;

public record RecipeSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string? ImageUrl,
    RecipeCategory Category,
    RecipeVisibility Visibility,
    int ServingSize,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    IReadOnlyList<string> Tags,
    string OwnerId,
    string OwnerDisplayName,
    DateTimeOffset CreatedAt
);
