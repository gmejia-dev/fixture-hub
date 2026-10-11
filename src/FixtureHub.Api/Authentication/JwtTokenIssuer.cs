using FixtureHub.Application.Abstractions.Authentication;
using FixtureHub.Application.Authentication.Login;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FixtureHub.Api.Authentication;

internal sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public AuthToken Issue(Guid userId, string username, string role)
    {
        var jwt = options.Value;
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var expiresAt = issuedAt.AddMinutes(jwt.ExpirationMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtClaims.Subject] = userId.ToString(),
                [JwtClaims.Name] = username,
                [JwtClaims.Role] = role,
                [JwtClaims.TokenId] = Guid.NewGuid().ToString()
            },
            SigningCredentials = new SigningCredentials(jwt.SecurityKey(), SecurityAlgorithms.HmacSha256)
        });

        return new AuthToken(token, AuthToken.BearerType, expiresAt);
    }
}
