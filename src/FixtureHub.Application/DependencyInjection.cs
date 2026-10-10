using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Decorators;
using FixtureHub.Application.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(
                classes => classes
                    .AssignableToAny(typeof(ICommandHandler<>), typeof(ICommandHandler<,>))
                    .NotInNamespaceOf(typeof(LoggingDecorator)),
                publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)), publicOnly: false)
            .AsImplementedInterfaces(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValidator<>))
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.Decorate(typeof(ICommandHandler<>), typeof(TransactionDecorator.CommandHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(TransactionDecorator.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));

        return services;
    }
}
