using System.Text.Json;

namespace RecipeBook.Blazor.Server.Services;

public class DemoAuthService : IDemoAuthService
{
    private readonly HttpClient _httpClient;

    public DemoAuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> LoginAsDemoAsync()
    {
        var response = await _httpClient.PostAsync("/api/v1/auth/demo", null);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonDocument.ParseAsync(stream);
        return result.RootElement.GetProperty("token").GetString()!;
    }
}
