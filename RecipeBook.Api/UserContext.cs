using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RecipeBook.Application.Interfaces;

namespace RecipeBook.Api;

public class UserControllerContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserControllerContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
