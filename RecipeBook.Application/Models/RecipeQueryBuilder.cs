using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.Models;

/// <summary>
/// Builds and validates a RecipeQuery from raw controller parameters.
/// </summary>
public static class RecipeQueryBuilder
{
    public static (string? Error, RecipeQuery? Query) Create(
        string? search, string? category, string? tags, 
        int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return ("Page and pageSize must be >= 1, pageSize must be <= 100.", null);

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search;
        
        RecipeCategory? parsedCategory = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<RecipeCategory>(category, ignoreCase: true, out var parsedCategoryValue))
                return ($"Invalid category: '{category}'. Valid values: {string.Join(", ", Enum.GetNames<RecipeCategory>())}", null);
            parsedCategory = parsedCategoryValue;
        }

        IReadOnlyList<string>? parsedTags = null;
        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .ToList();
            if (tagList.Count > 0)
                parsedTags = tagList;
        }

        return (null, new RecipeQuery(
            SearchText: normalizedSearch,
            Category: parsedCategory,
            Tags: parsedTags,
            Page: page,
            PageSize: pageSize));
    }
}
