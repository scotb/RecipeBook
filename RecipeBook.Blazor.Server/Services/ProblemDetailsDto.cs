using System.Text.Json.Serialization;

namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// Minimal RFC 7807 ProblemDetails DTO for deserializing API error responses.
/// </summary>
public class ProblemDetailsDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}
