using FluentAssertions;
using RecipeBook.Infrastructure.Config;

namespace RecipeBook.Infrastructure.Tests.Config;

public class JwtConfigTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesSuccessfully()
    {
        var act = () => new JwtConfig(
            issuer: "RecipeBook",
            audience: "RecipeBookAPI",
            secretKey: "a-very-long-and-secure-secret-key-at-least-32-chars");

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmptyIssuer_ThrowsInvalidOperationException()
    {
        var act = () => new JwtConfig(
            issuer: "",
            audience: "RecipeBookAPI",
            secretKey: "a-very-long-and-secure-secret-key-at-least-32-chars");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithEmptyAudience_ThrowsInvalidOperationException()
    {
        var act = () => new JwtConfig(
            issuer: "RecipeBook",
            audience: "",
            secretKey: "a-very-long-and-secure-secret-key-at-least-32-chars");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithEmptySecretKey_ThrowsInvalidOperationException()
    {
        var act = () => new JwtConfig(
            issuer: "RecipeBook",
            audience: "RecipeBookAPI",
            secretKey: "");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TokenLifetimeMinutes_DefaultsTo60()
    {
        var config = new JwtConfig(
            issuer: "RecipeBook",
            audience: "RecipeBookAPI",
            secretKey: "a-very-long-and-secure-secret-key-at-least-32-chars");

        config.TokenLifetimeMinutes.Should().Be(60);
    }

    // FIX 5, Test 1: SecretKey shorter than 32 characters throws InvalidOperationException
    [Fact]
    public void Constructor_WithShortSecretKey_ThrowsInvalidOperationException()
    {
        var act = () => new JwtConfig(
            issuer: "RecipeBook",
            audience: "RecipeBookAPI",
            secretKey: "short");
        act.Should().Throw<InvalidOperationException>();
    }

    // FIX 5, Test 2: SecretKey of exactly 32 characters is accepted
    [Fact]
    public void Constructor_With32CharSecretKey_AcceptsSuccessfully()
    {
        var secretKey = new string('x', 32);
        var act = () => new JwtConfig(
            issuer: "RecipeBook",
            audience: "RecipeBookAPI",
            secretKey: secretKey);
        act.Should().NotThrow();
    }

    // FIX 5, Test 3: SecretKey longer than 32 characters is accepted
    [Fact]
    public void Constructor_WithLongSecretKey_AcceptsSuccessfully()
    {
        var secretKey = new string('y', 64);
        var act = () => new JwtConfig(
            issuer: "RecipeBook",
            audience: "RecipeBookAPI",
            secretKey: secretKey);
        act.Should().NotThrow();
    }
}
