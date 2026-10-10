using System.Reflection;
using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Application.Abstractions.Messaging;
using FluentValidation;
using NetArchTest.Rules;

namespace FixtureHub.ArchitectureTests;

public class VisibilityTests
{
    private const string Application = "FixtureHub.Application";
    private const string Infrastructure = "FixtureHub.Infrastructure";

    [Fact]
    public void Infrastructure_EveryTypeExceptTheRegistrationAndTheMigrations_IsInternal()
    {
        var types = Types.InAssembly(Assembly.Load(Infrastructure))
            .That()
            .DoNotResideInNamespace($"{Infrastructure}.Persistence.Migrations")
            .And()
            .DoNotHaveName("DependencyInjection");

        AssertSelects(types, "UnitOfWork", "FixtureHubDbContext", "TeamRepository", "IdempotencyStore");
        AssertSuccessful(types.Should().NotBePublic().GetResult());
    }

    [Theory]
    [InlineData(typeof(ICommandHandler<>), "DeleteTeamHandler")]
    [InlineData(typeof(ICommandHandler<,>), "ScheduleMatchHandler")]
    [InlineData(typeof(IDomainEventHandler<>), "MatchFinishedHandler")]
    public void Application_HandlersOfTheContract_AreInternal(Type contract, string knownHandler)
    {
        var types = Types.InAssembly(Assembly.Load(Application))
            .That()
            .ImplementInterface(contract);

        AssertSelects(types, knownHandler);
        AssertSuccessful(types.Should().NotBePublic().GetResult());
    }

    [Fact]
    public void Application_Validators_AreInternal()
    {
        var types = Types.InAssembly(Assembly.Load(Application))
            .That()
            .Inherit(typeof(AbstractValidator<>));

        AssertSelects(types, "CreateTeamValidator", "CorrectMatchResultValidator");
        AssertSuccessful(types.Should().NotBePublic().GetResult());
    }

    private static void AssertSelects(PredicateList types, params string[] expectedNames)
    {
        var selected = types.GetTypes().Select(type => type.Name).ToList();

        Assert.All(expectedNames, name => Assert.Contains(name, selected));
    }

    private static void AssertSuccessful(TestResult result) =>
        Assert.True(
            result.IsSuccessful,
            "Tipos que deberían ser internal: " + string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
}
