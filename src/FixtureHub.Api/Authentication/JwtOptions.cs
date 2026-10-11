using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FixtureHub.Api.Authentication;

internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int SigningKeyMinBytes = 32;
    public const int MaxExpirationMinutes = 1440;

    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; }

    public SymmetricSecurityKey SecurityKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateLifetime = true,
        ClockSkew = ClockSkew,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SecurityKey(),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = JwtClaims.Name,
        RoleClaimType = JwtClaims.Role
    };
}
