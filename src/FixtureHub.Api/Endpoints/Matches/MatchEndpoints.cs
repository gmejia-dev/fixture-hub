using FixtureHub.Api.Http;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Matches.AnnulGoal;
using FixtureHub.Application.Matches.CancelMatch;
using FixtureHub.Application.Matches.CorrectMatchResult;
using FixtureHub.Application.Matches.DeleteMatch;
using FixtureHub.Application.Matches.FinishMatch;
using FixtureHub.Application.Matches.GetMatchById;
using FixtureHub.Application.Matches.GetMatches;
using FixtureHub.Application.Matches.RegisterGoal;
using FixtureHub.Application.Matches.RescheduleMatch;
using FixtureHub.Application.Matches.ScheduleMatch;
using FixtureHub.Application.Matches.StartMatch;
using FixtureHub.Application.Pagination;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixtureHub.Api.Endpoints.Matches;

internal static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder api)
    {
        var matches = api.MapGroup("/matches").WithTags("Matches");

        matches.MapGet("/", GetMatches).WithName(nameof(GetMatches)).WithSummary("Lista los partidos, paginados y filtrados por equipo, estado y fechas.");
        matches.MapGet("/{matchId:guid}", GetMatch).WithName(nameof(GetMatch)).WithSummary("Devuelve un partido con sus goles y correcciones.");

        matches.MapPost("/", ScheduleMatch).WithName(nameof(ScheduleMatch)).WithSummary("Programa un partido.");
        matches.MapPatch("/{matchId:guid}", RescheduleMatch).WithName(nameof(RescheduleMatch)).WithSummary("Cambia la fecha y hora de un partido programado.");
        matches.MapDelete("/{matchId:guid}", DeleteMatch).WithName(nameof(DeleteMatch)).WithSummary("Elimina un partido programado o cancelado.");

        matches.MapPost("/{matchId:guid}/start", StartMatch).WithName(nameof(StartMatch)).WithSummary("Inicia un partido programado.");
        matches.MapPost("/{matchId:guid}/finish", FinishMatch).WithName(nameof(FinishMatch)).WithSummary("Finaliza un partido en curso; actualiza la tabla.");
        matches.MapPost("/{matchId:guid}/cancel", CancelMatch).WithName(nameof(CancelMatch)).WithSummary("Cancela un partido programado o en curso.");

        matches.MapPost("/{matchId:guid}/goals", RegisterGoal).WithName(nameof(RegisterGoal)).WithSummary("Registra un gol en un partido en curso.");
        matches.MapPost("/{matchId:guid}/goals/{goalId:guid}/annul", AnnulGoal).WithName(nameof(AnnulGoal)).WithSummary("Anula un gol de un partido en curso.");
        matches.MapPut("/{matchId:guid}/result", CorrectMatchResult).WithName(nameof(CorrectMatchResult)).WithSummary("Corrige los goles de un partido finalizado.");

        return api;
    }

    private static async Task<Results<Ok<PagedResult<MatchSummary>>, ProblemHttpResult>> GetMatches(
        [AsParameters] PageParameters page,
        Guid? teamId,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        IQueryHandler<GetMatchesQuery, PagedResult<MatchSummary>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetMatchesQuery(page.Number, page.Size, page.SortBy, page.SortDirection, teamId, status, from, to),
            cancellationToken);

        return result.ToOk();
    }

    private static async Task<Results<Ok<MatchDetails>, ProblemHttpResult>> GetMatch(
        Guid matchId,
        IQueryHandler<GetMatchByIdQuery, MatchDetails> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMatchByIdQuery(matchId), cancellationToken);

        return result.ToOk();
    }

    private static async Task<Results<Created<CreatedResponse>, ProblemHttpResult>> ScheduleMatch(
        ScheduleMatchRequest request,
        ICommandHandler<ScheduleMatchCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ScheduleMatchCommand(request.HomeTeamId, request.AwayTeamId, request.ScheduledAt),
            cancellationToken);

        return result.ToCreated(matchId => $"/api/matches/{matchId}");
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RescheduleMatch(
        Guid matchId,
        RescheduleMatchRequest request,
        ICommandHandler<RescheduleMatchCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RescheduleMatchCommand(matchId, request.ScheduledAt), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteMatch(
        Guid matchId,
        ICommandHandler<DeleteMatchCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteMatchCommand(matchId), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartMatch(
        Guid matchId,
        ICommandHandler<StartMatchCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new StartMatchCommand(matchId), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> FinishMatch(
        Guid matchId,
        ICommandHandler<FinishMatchCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new FinishMatchCommand(matchId), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelMatch(
        Guid matchId,
        ICommandHandler<CancelMatchCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CancelMatchCommand(matchId), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<Created<CreatedResponse>, ProblemHttpResult>> RegisterGoal(
        Guid matchId,
        RegisterGoalRequest request,
        ICommandHandler<RegisterGoalCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterGoalCommand(matchId, request.PlayerId, request.Minute, request.IsOwnGoal),
            cancellationToken);

        return result.ToCreated(_ => $"/api/matches/{matchId}");
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AnnulGoal(
        Guid matchId,
        Guid goalId,
        ICommandHandler<AnnulGoalCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AnnulGoalCommand(matchId, goalId), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CorrectMatchResult(
        Guid matchId,
        CorrectMatchResultRequest request,
        ICommandHandler<CorrectMatchResultCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CorrectMatchResultCommand(matchId, request.Goals!, request.Reason),
            cancellationToken);

        return result.ToNoContent();
    }
}
