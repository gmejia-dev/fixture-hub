using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Matches.AnnulGoal;
using FixtureHub.Application.Matches.CancelMatch;
using FixtureHub.Application.Matches.FinishMatch;
using FixtureHub.Application.Matches.RegisterGoal;
using FixtureHub.Application.Matches.ScheduleMatch;
using FixtureHub.Application.Matches.StartMatch;
using FixtureHub.Application.Teams.AddPlayer;
using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;
using FixtureHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FixtureHub.Infrastructure.Seeding;

internal sealed partial class DatabaseSeeder(
    IServiceScopeFactory scopes,
    FixtureHubDbContext context,
    TimeProvider timeProvider,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Teams.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            LogSkipped(logger);
            return;
        }

        var teams = new List<Guid>();
        var players = new Dictionary<(int Team, int ShirtNumber), Guid>();

        foreach (var (name, country, roster) in SeedData.Teams)
        {
            var teamId = await SendAsync<CreateTeamCommand, Guid>(new CreateTeamCommand(name, country), cancellationToken);

            foreach (var (shirtNumber, playerName) in roster)
            {
                players[(teams.Count, shirtNumber)] = await SendAsync<AddPlayerCommand, Guid>(
                    new AddPlayerCommand(teamId, playerName, shirtNumber),
                    cancellationToken);
            }

            teams.Add(teamId);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(SeedData.Offset).DateTime);

        foreach (var match in SeedData.Matches)
            await PlayAsync(match, teams, players, today, cancellationToken);

        LogSeeded(logger, teams.Count, players.Count, SeedData.Matches.Count);
    }

    private async Task PlayAsync(
        SeedMatch match,
        List<Guid> teams,
        Dictionary<(int Team, int ShirtNumber), Guid> players,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var day = today.AddDays(7 * (match.Round - SeedData.CurrentRound));
        var scheduledAt = new DateTimeOffset(day.ToDateTime(match.KickOff), SeedData.Offset);

        var matchId = await SendAsync<ScheduleMatchCommand, Guid>(
            new ScheduleMatchCommand(teams[match.Home], teams[match.Away], scheduledAt),
            cancellationToken);

        if (match.Status == MatchStatus.Scheduled)
            return;

        if (match.Status == MatchStatus.Cancelled)
        {
            await SendAsync(new CancelMatchCommand(matchId), cancellationToken);
            return;
        }

        await SendAsync(new StartMatchCommand(matchId), cancellationToken);

        foreach (var goal in match.Goals)
        {
            var goalId = await SendAsync<RegisterGoalCommand, Guid>(
                new RegisterGoalCommand(matchId, players[(goal.Team, goal.ShirtNumber)], goal.Minute, goal.IsOwnGoal),
                cancellationToken);

            if (goal.IsAnnulled)
                await SendAsync(new AnnulGoalCommand(matchId, goalId), cancellationToken);
        }

        if (match.Status == MatchStatus.Finished)
            await SendAsync(new FinishMatchCommand(matchId), cancellationToken);
    }

    private async Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        await using var scope = scopes.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>()
            .HandleAsync(command, cancellationToken);

        return result.IsSuccess ? result.Value : throw Failed<TCommand>(result.Error);
    }

    private async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        await using var scope = scopes.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>()
            .HandleAsync(command, cancellationToken);

        if (result.IsFailure)
            throw Failed<TCommand>(result.Error);
    }

    private static InvalidOperationException Failed<TCommand>(Error error) =>
        new($"El seed falló en {typeof(TCommand).Name} con {error.Code}.");

    [LoggerMessage(Level = LogLevel.Information, Message = "La base ya tiene datos: se omite el seed")]
    private static partial void LogSkipped(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seed completado: {Teams} equipos, {Players} jugadores y {Matches} partidos")]
    private static partial void LogSeeded(ILogger logger, int teams, int players, int matches);
}
