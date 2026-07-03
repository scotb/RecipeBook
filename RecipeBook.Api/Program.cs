using RecipeBook.Api;
using RecipeBook.Api.Middleware;
using RecipeBook.Application;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register strongly-typed JWT configuration.
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<RecipeBook.Infrastructure.Config.JwtConfig>(jwtSection);

// Register auth services (TokenService + JWT Bearer) and chain Google/Facebook OAuth.
builder.Services.AddAuthServices(builder.Configuration)
    .AddGoogle(google =>
    {
        google.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        google.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        google.CallbackPath = "/auth/google/callback";
    })
    .AddFacebook(facebook =>
    {
        facebook.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
        facebook.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
        facebook.CallbackPath = "/auth/facebook/callback";
    });

// Register HttpContextAccessor so middleware and services can read the current user.
builder.Services.AddHttpContextAccessor();

// Register IUserContext implementation (scoped per request).
builder.Services.AddScoped<IUserContext, UserControllerContext>();

// Register authorization services (required by UseAuthorization()).
builder.Services.AddAuthorization();

// Register application-layer services.
builder.Services.AddApplicationServices();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
var app = builder.Build();
// Configure the HTTP req pipeline.
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseMiddleware<UnauthorizedHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.Run();
