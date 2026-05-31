namespace RecipeBook.Application.DTOs;

public record CreateMealPlanRequest(
    DateOnly WeekStartDate,
    string? Name
);
