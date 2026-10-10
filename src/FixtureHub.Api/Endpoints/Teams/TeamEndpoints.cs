using FixtureHub.Api.Http;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Teams.AddPlayer;
using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.Application.Teams.DeleteTeam;
using FixtureHub.Application.Teams.RemovePlayer;
using FixtureHub.Application.Teams.UpdatePlayer;
using FixtureHub.Application.Teams.UpdateTeam;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixtureHub.Api.Endpoints.Teams;

internal static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder api)
    {
        var teams = api.MapGroup("/teams").WithTags("Teams");

        teams.MapPost("/", CreateTeam).WithName(nameof(CreateTeam)).WithSummary("Crea un equipo.");
        teams.MapPut("/{teamId:guid}", UpdateTeam).WithName(nameof(UpdateTeam)).WithSummary("Cambia el nombre y el país de un equipo.");
        teams.MapDelete("/{teamId:guid}", DeleteTeam).WithName(nameof(DeleteTeam)).WithSummary("Elimina un equipo (borrado lógico).");

        teams.MapPost("/{teamId:guid}/players", AddPlayer).WithName(nameof(AddPlayer)).WithSummary("Agrega un jugador al equipo.");
        teams.MapPut("/{teamId:guid}/players/{playerId:guid}", UpdatePlayer).WithName(nameof(UpdatePlayer)).WithSummary("Cambia el nombre y el dorsal de un jugador.");
        teams.MapDelete("/{teamId:guid}/players/{playerId:guid}", RemovePlayer).WithName(nameof(RemovePlayer)).WithSummary("Quita un jugador del equipo.");

        return api;
    }

    private static async Task<Results<Created<CreatedResponse>, ProblemHttpResult>> CreateTeam(
        TeamRequest request,
        ICommandHandler<CreateTeamCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateTeamCommand(request.Name, request.Country), cancellationToken);

        return result.ToCreated(teamId => $"/api/teams/{teamId}");
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateTeam(
        Guid teamId,
        TeamRequest request,
        ICommandHandler<UpdateTeamCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateTeamCommand(teamId, request.Name, request.Country), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteTeam(
        Guid teamId,
        ICommandHandler<DeleteTeamCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteTeamCommand(teamId), cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<Created<CreatedResponse>, ProblemHttpResult>> AddPlayer(
        Guid teamId,
        PlayerRequest request,
        ICommandHandler<AddPlayerCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AddPlayerCommand(teamId, request.Name, request.ShirtNumber),
            cancellationToken);

        return result.ToCreated(_ => $"/api/teams/{teamId}");
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdatePlayer(
        Guid teamId,
        Guid playerId,
        PlayerRequest request,
        ICommandHandler<UpdatePlayerCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdatePlayerCommand(teamId, playerId, request.Name, request.ShirtNumber),
            cancellationToken);

        return result.ToNoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemovePlayer(
        Guid teamId,
        Guid playerId,
        ICommandHandler<RemovePlayerCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemovePlayerCommand(teamId, playerId), cancellationToken);

        return result.ToNoContent();
    }
}
