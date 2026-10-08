using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
