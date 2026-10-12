using System.Reflection;
using FixtureHub.Application.Abstractions.Messaging;
using NetArchTest.Rules;

namespace FixtureHub.ArchitectureTests;

public class CqrsTests
{
    private const string Application = "FixtureHub.Application";
    private const string Infrastructure = "FixtureHub.Infrastructure";

    [Fact]
    public void QueryHandlers_DoNotUseTheWriteSide()
    {
        var types = Types.InAssembly(Assembly.Load(Application))
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>));

        AssertSelects(types, "GetTeamsHandler", "GetStandingsHandler", "LoginHandler");
        AssertSuccessful(types
            .ShouldNot()
            .HaveDependencyOnAny($"{Application}.Abstractions.Persistence", $"{Application}.Abstractions.Idempotency")
            .GetResult());
    }

    [Theory]
    [InlineData(typeof(ICommandHandler<>), "FinishMatchHandler")]
    [InlineData(typeof(ICommandHandler<,>), "CreateTeamHandler")]
    public void CommandHandlers_DoNotUseTheReadSide(Type contract, string knownHandler)
    {
        var types = Types.InAssembly(Assembly.Load(Application))
            .That()
            .ImplementInterface(contract);

        AssertSelects(types, knownHandler);
        AssertSuccessful(types.ShouldNot().HaveDependencyOnAny($"{Application}.Abstractions.Reading").GetResult());
    }

    [Fact]
    public void Readers_UseDapperWithoutEntityFramework()
    {
        var types = Types.InAssembly(Assembly.Load(Infrastructure))
            .That()
            .ResideInNamespace($"{Infrastructure}.Reading");

        AssertSelects(types, "TeamReader", "StandingsReader", "PagedSql");
        AssertSuccessful(types.ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore").GetResult());
    }

    [Theory]
    [InlineData($"{Infrastructure}.Persistence", "UnitOfWork")]
    [InlineData($"{Infrastructure}.Idempotency", "IdempotencyStore")]
    public void WriteSide_UsesEntityFrameworkWithoutDapper(string writeNamespace, string knownType)
    {
        var types = Types.InAssembly(Assembly.Load(Infrastructure))
            .That()
            .ResideInNamespace(writeNamespace);

        AssertSelects(types, knownType);
        AssertSuccessful(types.ShouldNot().HaveDependencyOnAny("Dapper").GetResult());
    }

    private static void AssertSelects(PredicateList types, params string[] expectedNames)
    {
        var selected = types.GetTypes().Select(type => type.Name).ToList();

        Assert.All(expectedNames, name => Assert.Contains(name, selected));
    }

    private static void AssertSuccessful(TestResult result) =>
        Assert.True(
            result.IsSuccessful,
            "Tipos que mezclan la escritura y la lectura: " + string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
}
