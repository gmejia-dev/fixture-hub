using System.Reflection;
using Mono.Cecil;

namespace FixtureHub.ArchitectureTests;

public class PersistenceTests
{
    private static readonly string[] Layers =
        ["FixtureHub.Domain", "FixtureHub.Application", "FixtureHub.Infrastructure", "FixtureHub.Api"];

    [Fact]
    public void SaveChanges_IsCalledOnlyByTheUnitOfWork()
    {
        var callers = Layers.SelectMany(TypesThatCallSaveChanges).Distinct().ToList();

        Assert.Equal(["FixtureHub.Infrastructure.Persistence.UnitOfWork"], callers);
    }

    private static List<string> TypesThatCallSaveChanges(string layer)
    {
        using var module = ModuleDefinition.ReadModule(Assembly.Load(layer).Location);

        return module.GetTypes()
            .Where(CallsSaveChanges)
            .Select(type => OutermostType(type).FullName)
            .ToList();
    }

    private static bool CallsSaveChanges(TypeDefinition type) =>
        type.Methods
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions)
            .Any(instruction => instruction.Operand is MethodReference { Name: "SaveChanges" or "SaveChangesAsync" });

    private static TypeDefinition OutermostType(TypeDefinition type) =>
        type.DeclaringType is null ? type : OutermostType(type.DeclaringType);
}
