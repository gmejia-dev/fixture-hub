using FixtureHub.Application.Abstractions.Authentication;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Errors;
using FixtureHub.Domain.Common;
using Microsoft.Extensions.Logging;

namespace FixtureHub.Application.Authentication.Login;

internal sealed partial class LoginHandler(
    IUserReader users,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    ILogger<LoginHandler> logger) : IQueryHandler<LoginQuery, AuthToken>
{
    public async Task<Result<AuthToken>> HandleAsync(LoginQuery query, CancellationToken cancellationToken)
    {
        var user = await users.FindByUsernameAsync(query.Username, cancellationToken);

        var passwordMatches = passwordHasher.Verify(query.Password, user?.PasswordHash);
        if (user is null || !passwordMatches)
        {
            LogRejected(logger, query.Username);
            return AuthErrors.InvalidCredentials;
        }

        LogSucceeded(logger, user.Username);

        return tokenIssuer.Issue(user.UserId, user.Username, user.Role);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Inicio de sesión de {Username}")]
    private static partial void LogSucceeded(ILogger logger, string username);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Inicio de sesión rechazado para {Username}")]
    private static partial void LogRejected(ILogger logger, string username);
}
