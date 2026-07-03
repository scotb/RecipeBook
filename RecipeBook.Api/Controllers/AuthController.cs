using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Application.Interfaces;

namespace RecipeBook.Api.Controllers;

[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        return Ok(new { userId, email, name });
    }

    [HttpGet("callback")]
    public IActionResult Callback()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        var token = _tokenService.GenerateToken(userId, email, name, null, Enumerable.Empty<string>());

        return Ok(new { token });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok();
    }
}
