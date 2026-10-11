using System.Security.Claims;
using FixtureHub.Api.Authentication;
using FixtureHub.UnitTests.Application.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FixtureHub.UnitTests.Api.Authentication;

public class JwtTokenIssuerTests
{
    private static readonly JwtOptions Jwt = NewOptions("clave-de-firma-de-los-tests-unitarios-0123456789");

    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly JsonWebTokenHandler _handler = new();

    [Fact]
    public void Issue_ReturnsABearerTokenThatExpiresAfterTheConfiguredMinutes()
    {
        var token = Issuer(Jwt).Issue(Guid.NewGuid(), "admin", "Admin");

        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(_time.Now.ToUnixTimeSeconds()).AddMinutes(60), token.ExpiresAt);
    }

    [Fact]
    public async Task Issue_TheTokenIsValidForTheApiAndCarriesTheUserIdTheNameAndTheRole()
    {
        var userId = Guid.NewGuid();

        var token = Issuer(Jwt).Issue(userId, "admin", "Admin");
        var result = await _handler.ValidateTokenAsync(token.AccessToken, Jwt.ValidationParameters());

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(userId.ToString(), result.ClaimsIdentity.FindFirst("sub")?.Value);
        Assert.Equal("admin", result.ClaimsIdentity.Name);
        Assert.True(new ClaimsPrincipal(result.ClaimsIdentity).IsInRole("Admin"));
    }

    [Fact]
    public void Issue_SignsWithHmacSha256ForTheConfiguredIssuerAndAudienceWithAUniqueId()
    {
        var first = new JsonWebToken(Issuer(Jwt).Issue(Guid.NewGuid(), "admin", "Admin").AccessToken);
        var second = new JsonWebToken(Issuer(Jwt).Issue(Guid.NewGuid(), "admin", "Admin").AccessToken);

        Assert.Equal("HS256", first.Alg);
        Assert.Equal("FixtureHub", first.Issuer);
        Assert.Equal(["FixtureHub"], first.Audiences);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task ValidationParameters_RejectATokenSignedWithAnotherKey()
    {
        var token = Issuer(NewOptions("otra-clave-de-firma-que-la-api-no-conoce-0123456789")).Issue(Guid.NewGuid(), "admin", "Admin");

        var result = await _handler.ValidateTokenAsync(token.AccessToken, Jwt.ValidationParameters());

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidationParameters_RejectAnExpiredToken()
    {
        _time.Now = DateTimeOffset.UtcNow.AddHours(-2);

        var token = Issuer(Jwt).Issue(Guid.NewGuid(), "admin", "Admin");
        var result = await _handler.ValidateTokenAsync(token.AccessToken, Jwt.ValidationParameters());

        Assert.False(result.IsValid);
        Assert.IsType<SecurityTokenExpiredException>(result.Exception);
    }

    private JwtTokenIssuer Issuer(JwtOptions options) => new(Options.Create(options), _time);

    private static JwtOptions NewOptions(string signingKey) => new()
    {
        Issuer = "FixtureHub",
        Audience = "FixtureHub",
        SigningKey = signingKey,
        ExpirationMinutes = 60
    };
}
