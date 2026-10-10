using System.Reflection;
using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.ArchitectureTests;

public class ConventionTests
{
    private static readonly Type[] CommandContracts = [typeof(ICommand), typeof(ICommand<>)];

    private static readonly Type[] CommandHandlerContracts = [typeof(ICommandHandler<>), typeof(ICommandHandler<,>)];

    private static readonly Type[] QueryContracts = [typeof(IQuery<>)];

    private static readonly Type[] QueryHandlerContracts = [typeof(IQueryHandler<,>)];

    [Fact]
    public void EveryCommand_HasExactlyOneHandler()
    {
        var wrong = RequestsWithoutExactlyOneHandler(
            CommandContracts,
            CommandHandlerContracts,
            "CreateTeamCommand",
            "CorrectMatchResultCommand");

        Assert.True(!wrong.Any(), "Commands sin exactamente un handler: " + string.Join(", ", wrong));
    }

    [Fact]
    public void EveryQuery_HasExactlyOneHandler()
    {
        var wrong = RequestsWithoutExactlyOneHandler(
            QueryContracts,
            QueryHandlerContracts,
            "GetTeamsQuery",
            "GetTeamByIdQuery");

        Assert.True(!wrong.Any(), "Queries sin exactamente un handler: " + string.Join(", ", wrong));
    }

    private static List<string> RequestsWithoutExactlyOneHandler(
        Type[] requestContracts,
        Type[] handlerContracts,
        params string[] knownRequests)
    {
        var types = Assembly.Load("FixtureHub.Application").GetTypes();
        var requests = types.Where(type => type is { IsClass: true, IsAbstract: false } && Implements(type, requestContracts)).ToList();
        var handled = types
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(contract => contract.IsGenericType && handlerContracts.Contains(contract.GetGenericTypeDefinition()))
            .Select(contract => contract.GetGenericArguments()[0])
            .ToList();

        Assert.All(knownRequests, name => Assert.Contains(requests, request => request.Name == name));

        return requests
            .Select(request => (request.Name, Handlers: handled.Count(type => type == request)))
            .Where(request => request.Handlers != 1)
            .Select(request => $"{request.Name} ({request.Handlers} handlers)")
            .ToList();
    }

    private static bool Implements(Type type, Type[] contracts) =>
        type.GetInterfaces().Any(contract =>
            contracts.Contains(contract) || (contract.IsGenericType && contracts.Contains(contract.GetGenericTypeDefinition())));
}
