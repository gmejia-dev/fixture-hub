using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Infrastructure.Persistence;
using FixtureHub.Infrastructure.Persistence.Repositories;
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

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
