using System.Text;
using FixtureHub.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace FixtureHub.Api.Authentication;

internal static class AuthenticationServices
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(
                jwt => !string.IsNullOrWhiteSpace(jwt.Issuer) && !string.IsNullOrWhiteSpace(jwt.Audience),
                "Configura Jwt:Issuer y Jwt:Audience.")
            .Validate(
                jwt => Encoding.UTF8.GetByteCount(jwt.SigningKey) >= JwtOptions.SigningKeyMinBytes,
                $"Configura Jwt:SigningKey (al menos {JwtOptions.SigningKeyMinBytes} bytes) con user-secrets o la variable de entorno Jwt__SigningKey.")
            .Validate(
                jwt => jwt.ExpirationMinutes is >= 1 and <= JwtOptions.MaxExpirationMinutes,
                $"Jwt:ExpirationMinutes debe estar entre 1 y {JwtOptions.MaxExpirationMinutes}.")
            .ValidateOnStart();

        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = jwt.Value.ValidationParameters();
            });

        services.AddAuthorization();

        return services;
    }
}
