using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Infrastructure.Persistence;
using FixtureHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FixtureHubDbContext>(options => options.UseSqlServer(
            connectionString,
            sqlServer => sqlServer.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)));

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
