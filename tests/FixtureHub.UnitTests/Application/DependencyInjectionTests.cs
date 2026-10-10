using FixtureHub.Application;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Application.Decorators;
using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.Application.Teams.DeleteTeam;
using FixtureHub.Domain.Common;
using FixtureHub.UnitTests.Application.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace FixtureHub.UnitTests.Application;

public sealed class DependencyInjectionTests : IDisposable
{
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTeamRepository _teams = new();
    private readonly ServiceProvider _provider;

    public DependencyInjectionTests()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<ITeamRepository>(_teams);
        services.AddSingleton<IMatchRepository>(new FakeMatchRepository());
        services.AddApplication();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    [Fact]
    public void AddApplication_CommandWithResponse_IsResolvedWithLoggingAsTheOutermostDecorator()
    {
        using var scope = _provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateTeamCommand, Guid>>();

        Assert.IsType<LoggingDecorator.CommandHandler<CreateTeamCommand, Guid>>(handler);
    }

    [Fact]
    public void AddApplication_CommandWithoutResponse_IsResolvedWithLoggingAsTheOutermostDecorator()
    {
        using var scope = _provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteTeamCommand>>();

        Assert.IsType<LoggingDecorator.CommandHandler<DeleteTeamCommand>>(handler);
    }

    [Fact]
    public void AddApplication_Decorators_AreNotRegisteredAsHandlers()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType?.Namespace == typeof(LoggingDecorator).Namespace);
    }

    [Fact]
    public async Task AddApplication_InvalidCommand_IsLoggedAndRejectedBeforeOpeningATransaction()
    {
        var result = await HandleAsync(new CreateTeamCommand("", "El Salvador"));

        Assert.Equal("Validation.Failed", result.Error.Code);
        Assert.Empty(_unitOfWork.Calls);
        Assert.Empty(_teams.All);
        Assert.Contains(
            _provider.GetFakeLogCollector().GetSnapshot(),
            record => record.Level == LogLevel.Warning
                && record.Message.Contains("Validation.Failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddApplication_ValidCommand_RunsTheHandlerInsideATransaction()
    {
        var result = await HandleAsync(new CreateTeamCommand("Los Halcones", "El Salvador"));

        Assert.True(result.IsSuccess);
        Assert.Equal(["Begin", "Commit"], _unitOfWork.Calls);
        Assert.Single(_teams.All);
    }

    public void Dispose() => _provider.Dispose();

    private async Task<Result<Guid>> HandleAsync(CreateTeamCommand command)
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateTeamCommand, Guid>>();

        return await handler.HandleAsync(command, CancellationToken.None);
    }
}
