namespace RecipeBook.Infrastructure.Config;

public class JwtConfig
{
    public string Issuer { get; }
    public string Audience { get; }
    public string SecretKey { get; }
    public int TokenLifetimeMinutes { get; set; } = 60;

    public JwtConfig(string issuer, string audience, string secretKey)
    {
        EnsureNotEmpty(issuer, nameof(issuer));
        EnsureNotEmpty(audience, nameof(audience));
        EnsureNotEmpty(secretKey, nameof(secretKey));
        if (secretKey.Length < 32)
            throw new InvalidOperationException("SecretKey must be at least 32 characters long.");

        Issuer = issuer;
        Audience = audience;
        SecretKey = secretKey;
    }

    private static void EnsureNotEmpty(string value, string name)
    {
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"{name} is required.");
    }
}
