using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using RecipeBook.Blazor.Server.Components;
using RecipeBook.Blazor.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor / Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// Authorization — require authentication by default; unauthenticated → redirect to /
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Authentication — JWT-based (replaces Identity scaffold)
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateService>();

// MudBlazor
builder.Services.AddMudServices();

// API Clients
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration key is required.");

// Demo Auth — unauthenticated client (no Bearer handler)
builder.Services.AddHttpClient<IDemoAuthService, DemoAuthService>("DemoAuth", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// API Clients
builder.Services.AddHttpClient<IRecipeApiService, RecipeApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

builder.Services.AddHttpClient<IMealPlanApiService, MealPlanApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    // Development-only middleware can be added here.
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
