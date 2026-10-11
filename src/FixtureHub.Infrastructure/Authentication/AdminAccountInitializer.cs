using FixtureHub.Application.Abstractions.Authentication;
using FixtureHub.Application.Authentication;
using FixtureHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FixtureHub.Infrastructure.Authentication;

internal sealed partial class AdminAccountInitializer(
    FixtureHubDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<AdminOptions> options,
    ILogger<AdminAccountInitializer> logger)
{
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        var admin = options.Value;
        var account = await context.Users.FirstOrDefaultAsync(user => user.Username == admin.Username, cancellationToken);

        if (account is null)
        {
            context.Users.Add(new UserAccount(Guid.CreateVersion7(), admin.Username, passwordHasher.Hash(admin.Password), Roles.Admin));
            await context.SaveChangesAsync(cancellationToken);
            LogCreated(logger, admin.Username);
            return;
        }

        if (passwordHasher.Verify(admin.Password, account.PasswordHash))
            return;

        account.ChangePasswordHash(passwordHasher.Hash(admin.Password));
        await context.SaveChangesAsync(cancellationToken);
        LogPasswordChanged(logger, admin.Username);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Administrador {Username} creado")]
    private static partial void LogCreated(ILogger logger, string username);

    [LoggerMessage(Level = LogLevel.Warning, Message = "La contraseña del administrador {Username} cambió en la configuración: se actualizó su hash")]
    private static partial void LogPasswordChanged(ILogger logger, string username);
}
