using System.Text.Json.Serialization;

namespace RecipeBook.Blazor.Server.Services;

internal class PagedResultDto<T>
{
    [JsonPropertyName("items")]
    public IReadOnlyList<T>? Items { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}
