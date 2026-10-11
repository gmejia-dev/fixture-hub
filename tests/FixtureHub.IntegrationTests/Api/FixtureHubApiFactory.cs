using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Api;

public sealed class FixtureHubApiFactory(
    string connectionString,
    IReadOnlyDictionary<string, string>? settings = null,
    Action<IServiceCollection>? configureServices = null)
    : WebApplicationFactory<Program>
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "contraseña-de-prueba";
    public const string SigningKey = "clave-de-firma-de-los-tests-de-integracion-0123456789";

    private static readonly Dictionary<string, string> DefaultSettings = new()
    {
        ["Admin:Username"] = AdminUsername,
        ["Admin:Password"] = AdminPassword,
        ["Jwt:SigningKey"] = SigningKey
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:FixtureHub", connectionString);

        foreach (var (key, value) in DefaultSettings.Concat(settings ?? new Dictionary<string, string>()))
            builder.UseSetting(key, value);

        if (configureServices is not null)
            builder.ConfigureTestServices(configureServices);
    }
}
