using FixtureHub.IntegrationTests.Api;

namespace FixtureHub.IntegrationTests.Authentication;

[Collection(nameof(SqlServerCollection))]
public sealed class StartupConfigurationTests(SqlServerFixture database)
{
    [Theory]
    [InlineData("Admin:Username", "", "Admin:Username")]
    [InlineData("Admin:Password", "", "Admin:Password")]
    [InlineData("Admin:Password", "corta", "Admin:Password")]
    [InlineData("Jwt:SigningKey", "", "Jwt:SigningKey")]
    [InlineData("Jwt:SigningKey", "clave-de-menos-de-32-bytes", "Jwt:SigningKey")]
    [InlineData("Jwt:Issuer", "", "Jwt:Issuer")]
    [InlineData("Jwt:ExpirationMinutes", "0", "Jwt:ExpirationMinutes")]
    public async Task Startup_WithAMissingOrInvalidSetting_FailsNamingTheSetting(string key, string value, string expected)
    {
        await using var api = new FixtureHubApiFactory(database.ConnectionString, new Dictionary<string, string> { [key] = value });

        var exception = Record.Exception(() => api.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains(expected, exception.ToString(), StringComparison.Ordinal);
    }
}
