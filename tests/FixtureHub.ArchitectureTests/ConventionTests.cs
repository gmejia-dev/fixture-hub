using System.Reflection;
using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.ArchitectureTests;

public class ConventionTests
{
    private static readonly Type[] CommandContracts = [typeof(ICommand), typeof(ICommand<>)];

    private static readonly Type[] HandlerContracts = [typeof(ICommandHandler<>), typeof(ICommandHandler<,>)];

    [Fact]
    public void EveryCommand_HasExactlyOneHandler()
    {
        var types = Assembly.Load("FixtureHub.Application").GetTypes();
        var commands = types.Where(type => type is { IsClass: true, IsAbstract: false } && Implements(type, CommandContracts)).ToList();
        var handledCommands = types
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(contract => contract.IsGenericType && HandlerContracts.Contains(contract.GetGenericTypeDefinition()))
            .Select(contract => contract.GetGenericArguments()[0])
            .ToList();

        Assert.Contains(commands, command => command.Name == "CreateTeamCommand");
        Assert.Contains(commands, command => command.Name == "CorrectMatchResultCommand");

        var wrong = commands
            .Select(command => (command.Name, Handlers: handledCommands.Count(handled => handled == command)))
            .Where(command => command.Handlers != 1)
            .Select(command => $"{command.Name} ({command.Handlers} handlers)");

        Assert.True(!wrong.Any(), "Commands sin exactamente un handler: " + string.Join(", ", wrong));
    }

    private static bool Implements(Type type, Type[] contracts) =>
        type.GetInterfaces().Any(contract =>
            contracts.Contains(contract) || (contract.IsGenericType && contracts.Contains(contract.GetGenericTypeDefinition())));
}
