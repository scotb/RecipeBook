namespace RecipeBook.Application.DTOs;

public record MealPlanDto(
    Guid Id,
    string? Name,
    DateOnly WeekStartDate,
    DateTimeOffset CreatedAt,
    IReadOnlyList<MealEntryDto> Entries
);
