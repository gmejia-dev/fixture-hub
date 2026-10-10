using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Decorators;
using FixtureHub.Domain.Common;
using FluentValidation;

namespace FixtureHub.UnitTests.Application.Decorators;

public class ValidationDecoratorTests
{
    private readonly CountingHandler _handler = new();

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsTheHandler()
    {
        var decorator = Decorate(new RegisterThingValidator());

        var result = await decorator.HandleAsync(new RegisterThing("Copa", 3, []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _handler.Calls);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsTheCodesOfEachFieldWithoutCallingTheHandler()
    {
        var decorator = Decorate(new RegisterThingValidator());

        var result = await decorator.HandleAsync(new RegisterThing("", 0, []), CancellationToken.None);

        Assert.Equal("Validation.Failed", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        var errors = ErrorsByField(result.Error);
        Assert.Equal(["Thing.NameRequired"], errors["name"]);
        Assert.Equal(["Thing.InvalidSize"], errors["size"]);
        Assert.Equal(0, _handler.Calls);
    }

    [Fact]
    public async Task HandleAsync_InvalidNestedItem_UsesCamelCaseInTheWholePath()
    {
        var decorator = Decorate(new RegisterThingValidator());

        var result = await decorator.HandleAsync(
            new RegisterThing("Copa", 3, [new ThingPart(0)]),
            CancellationToken.None);

        Assert.Equal(["Thing.InvalidPartQuantity"], ErrorsByField(result.Error)["parts[0].quantity"]);
    }

    [Fact]
    public async Task HandleAsync_WithoutValidators_CallsTheHandler()
    {
        var decorator = Decorate();

        var result = await decorator.HandleAsync(new RegisterThing("", 0, []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _handler.Calls);
    }

    private ValidationDecorator.CommandHandler<RegisterThing, Guid> Decorate(
        params IValidator<RegisterThing>[] validators) =>
        new(_handler, validators);

    private static IReadOnlyDictionary<string, string[]> ErrorsByField(Error error) =>
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(error.Metadata?["errors"]);

    private sealed record ThingPart(int Quantity);

    private sealed record RegisterThing(string Name, int Size, IReadOnlyList<ThingPart> Parts) : ICommand<Guid>;

    private sealed class RegisterThingValidator : AbstractValidator<RegisterThing>
    {
        public RegisterThingValidator()
        {
            RuleFor(command => command.Name).NotEmpty().WithErrorCode("Thing.NameRequired");
            RuleFor(command => command.Size).GreaterThan(0).WithErrorCode("Thing.InvalidSize");
            RuleForEach(command => command.Parts).ChildRules(part =>
                part.RuleFor(p => p.Quantity).GreaterThan(0).WithErrorCode("Thing.InvalidPartQuantity"));
        }
    }

    private sealed class CountingHandler : ICommandHandler<RegisterThing, Guid>
    {
        public int Calls { get; private set; }

        public Task<Result<Guid>> HandleAsync(RegisterThing command, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result.Success(Guid.NewGuid()));
        }
    }
}
