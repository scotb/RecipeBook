using System.Net;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;

namespace RecipeBook.Blazor.Server.Services;

public class RecipeApiService : ApiClientServiceBase, IRecipeApiService
{
    private readonly HttpClient _httpClient;

    public RecipeApiService(HttpClient httpClient, IAuthStateService authStateService)
        : base(httpClient, authStateService)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<RecipeSummaryDto>> GetPublicRecipesAsync(
        string? search, string? category, string? tags, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(tags)) query.Add($"tags={Uri.EscapeDataString(tags)}");

        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/recipes/public?{string.Join("&", query)}"), ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to fetch recipes");
        }
        var dto = await response.Content.ReadFromJsonAsync<PagedResultDto<RecipeSummaryDto>>(ct);
        return new PagedResult<RecipeSummaryDto>(dto!.Items!, dto.TotalCount, dto.Page, dto.PageSize);
    }

    public async Task<PagedResult<RecipeSummaryDto>> GetMyRecipesAsync(
        string? search, string? category, string? tags, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(tags)) query.Add($"tags={Uri.EscapeDataString(tags)}");

        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/recipes/my?{string.Join("&", query)}"), ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to fetch my recipes");
        }
        var dto = await response.Content.ReadFromJsonAsync<PagedResultDto<RecipeSummaryDto>>(ct);
        return new PagedResult<RecipeSummaryDto>(dto!.Items!, dto.TotalCount, dto.Page, dto.PageSize);
    }

    public async Task<RecipeDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/recipes/{id}"), ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to fetch recipe");
        }
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(ct);
        return dto!;
    }

    public async Task<RecipeDto> CreateAsync(CreateRecipeRequest request, CancellationToken ct = default)
    {
        var content = JsonContent.Create(request);
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/recipes") { Content = content }, ct);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(ct);
            return dto!;
        }
        throw new ApiException("Failed to create recipe");
    }

    public async Task<RecipeDto> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken ct = default)
    {
        var content = JsonContent.Create(request);
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recipes/{id}") { Content = content }, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to update recipe");
        }
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(ct);
        return dto!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{id}"), ct);
        if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
        {
            throw new ApiException("Failed to delete recipe");
        }
    }

    public async Task<RecipeDto> ForkAsync(Guid id, CancellationToken ct = default)
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{id}/fork"), ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to fork recipe");
        }
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(ct);
        return dto!;
    }

    public async Task<ImportRecipeRequest> ImportAsync(string url, CancellationToken ct = default)
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/recipes/import")
        {
            Content = JsonContent.Create(new { url })
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to import recipe");
        }
        var dto = await response.Content.ReadFromJsonAsync<ImportRecipeRequest>(ct);
        return dto!;
    }
}
