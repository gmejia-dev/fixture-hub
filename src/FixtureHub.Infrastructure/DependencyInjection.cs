using FixtureHub.Application.Abstractions.Authentication;
using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Infrastructure.Authentication;
using FixtureHub.Infrastructure.Idempotency;
using FixtureHub.Infrastructure.Persistence;
using FixtureHub.Infrastructure.Persistence.Repositories;
using FixtureHub.Infrastructure.Reading;
using FixtureHub.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FixtureHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FixtureHubDbContext>(options => options.UseSqlServer(
            connectionString,
            sqlServer => sqlServer.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)));

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ITeamStandingRepository, TeamStandingRepository>();

        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddHostedService<ExpiredIdempotencyRecordsCleanup>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton(new SqlConnectionFactory(connectionString));
        services.AddScoped<ITeamReader, TeamReader>();
        services.AddScoped<IMatchReader, MatchReader>();
        services.AddScoped<IStandingsReader, StandingsReader>();
        services.AddScoped<ITopScorerReader, TopScorerReader>();
        services.AddScoped<IUserReader, UserReader>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddOptions<AdminOptions>()
            .BindConfiguration(AdminOptions.SectionName)
            .Validate(
                admin => !string.IsNullOrWhiteSpace(admin.Username) && admin.Username.Length <= UserAccount.UsernameMaxLength,
                $"Configura Admin:Username (hasta {UserAccount.UsernameMaxLength} caracteres) con user-secrets o la variable de entorno Admin__Username.")
            .Validate(
                admin => admin.Password.Length >= AdminOptions.PasswordMinLength,
                $"Configura Admin:Password (al menos {AdminOptions.PasswordMinLength} caracteres) con user-secrets o la variable de entorno Admin__Password.")
            .ValidateOnStart();
        services.AddScoped<AdminAccountInitializer>();

        services.AddScoped<DatabaseSeeder>();

        services.AddHealthChecks().AddDbContextCheck<FixtureHubDbContext>();

        return services;
    }
}
