using RecipeBook.Application;
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

// Register application-layer services.
builder.Services.AddApplicationServices();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var app = builder.Build();
// Configure the HTTP req pipeline.
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Run();
