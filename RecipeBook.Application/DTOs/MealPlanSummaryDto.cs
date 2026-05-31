namespace RecipeBook.Application.DTOs;

public record MealPlanSummaryDto(
    Guid Id,
    string? Name,
    DateOnly WeekStartDate,
    DateTimeOffset CreatedAt,
    int EntryCount
);
