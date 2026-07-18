using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using RecipeBook.Application.Interfaces;

namespace RecipeBook.Api.Controllers;

[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public AuthController(ITokenService tokenService, IWebHostEnvironment hostingEnvironment)
    {
        _tokenService = tokenService;
        _hostingEnvironment = hostingEnvironment;
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

    [HttpPost("demo")]
    public IActionResult Demo()
    {
        if (!_hostingEnvironment.IsDevelopment())
            return NotFound();

        var token = _tokenService.GenerateToken(
            "demo-user-001", "demo@localhost", "Demo User", null, new[] { "User", "Admin" });

        return Ok(new { token });
    }
}
