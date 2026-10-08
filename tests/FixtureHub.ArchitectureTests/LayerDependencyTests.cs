using System.Reflection;
using NetArchTest.Rules;

namespace FixtureHub.ArchitectureTests;

public class LayerDependencyTests
{
    private const string Domain = "FixtureHub.Domain";
    private const string Application = "FixtureHub.Application";
    private const string Infrastructure = "FixtureHub.Infrastructure";
    private const string Api = "FixtureHub.Api";

    [Fact]
    public void Domain_DoesNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(Assembly.Load(Domain))
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Api)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        var result = Types.InAssembly(Assembly.Load(Application))
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(Assembly.Load(Infrastructure))
            .ShouldNot()
            .HaveDependencyOnAny(Api)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Domain_DoesNotDependOnDataAccessOrWebFrameworks()
    {
        var result = Types.InAssembly(Assembly.Load(Domain))
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Dapper",
                "System.Data",
                "Microsoft.Data.SqlClient",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.DependencyInjection",
                "System.Text.Json")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_DoesNotDependOnDataAccessOrWebFrameworks()
    {
        var result = Types.InAssembly(Assembly.Load(Application))
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Dapper",
                "System.Data",
                "Microsoft.Data.SqlClient",
                "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result);
    }

    private static void AssertSuccessful(TestResult result) =>
        Assert.True(
            result.IsSuccessful,
            "Tipos que rompen la regla: " + string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
}
