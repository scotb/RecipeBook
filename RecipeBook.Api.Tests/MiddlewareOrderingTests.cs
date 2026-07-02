using FluentAssertions;

namespace RecipeBook.Api.Tests;

public class MiddlewareOrderingTests
{
    [Fact]
    public void HttpsRedirectionBeforeAuthentication()
    {
        var programPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RecipeBook.Api", "Program.cs");
        if (!File.Exists(programPath))
            programPath = Path.GetFullPath("../../../RecipeBook.Api/Program.cs");
        
        var source = File.ReadAllText(programPath);
        
        var httpsRedirectionIndex = source.IndexOf("UseHttpsRedirection()");
        var authenticationIndex = source.IndexOf("UseAuthentication()");
        
        httpsRedirectionIndex.Should().BeGreaterThan(-1, "UseHttpsRedirection() should be present in Program.cs");
        authenticationIndex.Should().BeGreaterThan(-1, "UseAuthentication() should be present in Program.cs");
        httpsRedirectionIndex.Should().BeLessThan(authenticationIndex, 
            "UseHttpsRedirection() must come before UseAuthentication() in the middleware pipeline");
    }
}
