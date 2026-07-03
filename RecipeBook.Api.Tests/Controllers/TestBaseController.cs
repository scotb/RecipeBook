using Microsoft.AspNetCore.Mvc;
using RecipeBook.Api.Controllers;

namespace RecipeBook.Api.Tests.Controllers;

/// <summary>
/// Concrete subclass of BaseController for testing protected methods.
/// </summary>
public class TestBaseController : BaseController
{
    public new IActionResult Problem(int statusCode, string detail)
        => base.Problem(statusCode, detail);

    public new string GetUserId()
        => base.GetUserId();
}
