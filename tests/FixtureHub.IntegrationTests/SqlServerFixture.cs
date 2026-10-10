using FixtureHub.Application;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;
using FixtureHub.Infrastructure;
using FixtureHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace FixtureHub.IntegrationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private ServiceProvider? _provider;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "FixtureHub"
        }.ConnectionString;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(connectionString);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        await using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>().Database.MigrateAsync();
    }

    public AsyncServiceScope CreateScope() =>
        (_provider ?? throw new InvalidOperationException("La base de datos no se inició.")).CreateAsyncScope();

    public async Task<Result> SendAsync<TCommand>(TCommand command)
        where TCommand : ICommand
    {
        await using var scope = CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>()
            .HandleAsync(command, CancellationToken.None);
    }

    public async Task<Result<TResponse>> SendAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommand<TResponse>
    {
        await using var scope = CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>()
            .HandleAsync(command, CancellationToken.None);
    }

    public async Task SaveAsync(Team team)
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>();

        context.Teams.Add(team);
        await context.SaveChangesAsync();
    }

    public async Task<Team?> FindTeamAsync(Guid teamId)
    {
        await using var scope = CreateScope();

        return await scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>().Teams
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(team => team.Players)
            .FirstOrDefaultAsync(team => team.Id == teamId);
    }

    public async Task<Match?> FindMatchAsync(Guid matchId)
    {
        await using var scope = CreateScope();

        return await scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>().Matches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(match => match.Goals)
            .Include(match => match.Corrections)
            .FirstOrDefaultAsync(match => match.Id == matchId);
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
            await _provider.DisposeAsync();

        await _container.DisposeAsync();
    }
}
