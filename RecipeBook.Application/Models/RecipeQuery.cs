using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.Models;

public record RecipeQuery(
    string? SearchText = null,
    RecipeCategory? Category = null,
    IReadOnlyList<string>? Tags = null,
    int Page = 1,
    int PageSize = 20
);
