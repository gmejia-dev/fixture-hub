using Microsoft.IdentityModel.JsonWebTokens;

namespace FixtureHub.Api.Authentication;

internal static class JwtClaims
{
    public const string Subject = JwtRegisteredClaimNames.Sub;
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string Role = "role";
    public const string TokenId = JwtRegisteredClaimNames.Jti;
}
