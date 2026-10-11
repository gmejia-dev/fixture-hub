using System.Net.Http.Headers;
using System.Net.Http.Json;
using FixtureHub.Application.Authentication.Login;
using FixtureHub.IntegrationTests.Api;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace FixtureHub.IntegrationTests.Observability;

public sealed class ObservedApi : IAsyncDisposable
{
    private readonly FixtureHubApiFactory _factory;

    public ObservedApi(SqlServerFixture database, Action<IServiceCollection>? configureServices = null)
    {
        _factory = new FixtureHubApiFactory(
            database.ConnectionString,
            configureServices: services =>
            {
                services.AddSingleton<ILogEventSink>(Logs);
                configureServices?.Invoke(services);
            });
    }

    public CapturingSink Logs { get; } = new();

    public HttpClient CreateAnonymousClient() => _factory.CreateClient();

    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.LoginAsync(FixtureHubApiFactory.AdminUsername, FixtureHubApiFactory.AdminPassword);
        var token = (await login.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<AuthToken>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthToken.BearerType, token.AccessToken);

        return client;
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
