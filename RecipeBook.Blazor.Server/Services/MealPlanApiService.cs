using System.Net;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Blazor.Server.Services;

public class MealPlanApiService : ApiClientServiceBase, IMealPlanApiService
{
    private readonly HttpClient _httpClient;

    public MealPlanApiService(HttpClient httpClient, IAuthStateService authStateService)
        : base(httpClient, authStateService)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<MealPlanSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/v1/mealplans"), ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to fetch meal plans");
        }
        var dtos = await response.Content.ReadFromJsonAsync<MealPlanSummaryDto[]>(ct);
        return dtos!;
    }

    public Task<MealPlanDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public async Task<MealPlanDto> CreateAsync(CreateMealPlanRequest request, CancellationToken ct = default)
    {
        var content = JsonContent.Create(request);
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/mealplans") { Content = content }, ct);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var dto = await response.Content.ReadFromJsonAsync<MealPlanDto>(ct);
            return dto!;
        }
        throw new ApiException("Failed to create meal plan");
    }

    public Task<MealPlanDto> UpdateAsync(Guid id, UpdateMealPlanRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public async Task<MealEntryDto> SetEntryAsync(Guid id, SetMealEntryRequest request, CancellationToken ct = default)
    {
        var content = JsonContent.Create(request);
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/v1/mealplans/{id}/entries") { Content = content }, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("Failed to set meal entry");
        }
        var dto = await response.Content.ReadFromJsonAsync<MealEntryDto>(ct);
        return dto!;
    }

    public async Task DeleteEntryAsync(Guid id, ClearMealEntryRequest request, CancellationToken ct = default)
    {
        var content = JsonContent.Create(request);
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/mealplans/{id}/entries") { Content = content }, ct);
        if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
        {
            throw new ApiException("Failed to delete meal entry");
        }
    }
}
