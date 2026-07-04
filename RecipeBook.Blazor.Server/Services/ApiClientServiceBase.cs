using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using RecipeBook.Application.Exceptions;

namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// Base class for API services. Handles common HTTP error patterns from ProblemDetails responses.
/// </summary>
public abstract class ApiClientServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IAuthStateService _authStateService;

    public string? PendingRedirectUri { get; private set; }

    protected ApiClientServiceBase(HttpClient httpClient, IAuthStateService authStateService)
    {
        _httpClient = httpClient;
        _authStateService = authStateService;
    }

    /// <summary>
    /// Sends the request and handles common HTTP error responses (401, 403, ProblemDetails).
    /// </summary>
    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        PendingRedirectUri = null;

        var response = await _httpClient.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authStateService.ClearUser();
            PendingRedirectUri = "/login";
        }
        else if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _authStateService.ClearUser();
            PendingRedirectUri = "/forbidden";
        }

        if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/problem+json")
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(default);
            throw new ApiException(problemDetails?.Detail ?? "An error occurred");
        }

        return response;
    }
}
