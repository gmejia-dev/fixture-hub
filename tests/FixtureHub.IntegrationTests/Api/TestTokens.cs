using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FixtureHub.IntegrationTests.Api;

internal static class TestTokens
{
    public static string Create(
        string role = "Admin",
        Guid? userId = null,
        DateTimeOffset? expiresAt = null,
        string signingKey = FixtureHubApiFactory.SigningKey,
        string audience = "FixtureHub",
        bool signed = true)
    {
        var expires = (expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(10)).UtcDateTime;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "FixtureHub",
            Audience = audience,
            IssuedAt = expires.AddHours(-1),
            NotBefore = expires.AddHours(-1),
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = (userId ?? Guid.NewGuid()).ToString(),
                ["name"] = "usuario-de-prueba",
                ["role"] = role
            },
            SigningCredentials = signed
                ? new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)
                : null
        });
    }
}
