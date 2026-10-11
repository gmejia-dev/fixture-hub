using System.Net;
using System.Net.Http.Json;
using FixtureHub.Application.Authentication.Login;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class AuthEndpointsTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    [Fact]
    public async Task Login_WithTheAdminCredentials_Returns200WithABearerTokenWithoutAskingForAnIdempotencyKey()
    {
        var response = await _client.LoginAsync(FixtureHubApiFactory.AdminUsername, FixtureHubApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<AuthToken>();
        Assert.Equal("Bearer", token!.TokenType);
        Assert.InRange(token.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(59), DateTimeOffset.UtcNow.AddMinutes(61));
        var jwt = new JsonWebToken(token.AccessToken);
        Assert.Equal("admin", jwt.GetClaim("name").Value);
        Assert.Equal("Admin", jwt.GetClaim("role").Value);
    }

    [Fact]
    public async Task Login_WithAWrongPassword_Returns401InvalidCredentials()
    {
        var response = await _client.LoginAsync(FixtureHubApiFactory.AdminUsername, "otra-contraseña");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.InvalidCredentials", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task Login_WithAnUnknownUser_ReturnsTheSameAsAWrongPassword()
    {
        var unknown = await _client.LoginAsync("nadie", FixtureHubApiFactory.AdminPassword);
        var wrongPassword = await _client.LoginAsync(FixtureHubApiFactory.AdminUsername, "otra-contraseña");

        Assert.Equal(wrongPassword.StatusCode, unknown.StatusCode);
        var unknownProblem = await unknown.ReadProblemAsync();
        var wrongPasswordProblem = await wrongPassword.ReadProblemAsync();
        Assert.Equal(wrongPasswordProblem.ErrorCode(), unknownProblem.ErrorCode());
        Assert.Equal(wrongPasswordProblem.GetProperty("detail").GetString(), unknownProblem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_WithoutUsernameOrPassword_Returns400WithTheCodesPerField()
    {
        var response = await _client.LoginAsync("", "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).Metadata().GetProperty("errors");
        Assert.Equal("Auth.UsernameRequired", errors.GetProperty("username")[0].GetString());
        Assert.Equal("Auth.PasswordRequired", errors.GetProperty("password")[0].GetString());
    }
}
