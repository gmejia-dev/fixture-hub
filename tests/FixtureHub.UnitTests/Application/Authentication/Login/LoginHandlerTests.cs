using FixtureHub.Application.Authentication;
using FixtureHub.Application.Authentication.Login;
using FixtureHub.Domain.Common;
using FixtureHub.UnitTests.Application.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace FixtureHub.UnitTests.Application.Authentication.Login;

public class LoginHandlerTests
{
    private const string Password = "contraseña-de-prueba";

    private static readonly UserCredentials Admin = new(Guid.NewGuid(), "admin", $"hash:{Password}", Roles.Admin);

    private readonly FakeUserReader _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeLogger<LoginHandler> _logger = new();

    public LoginHandlerTests() => _users.Users.Add(Admin);

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsTheTokenOfTheUser()
    {
        var result = await Handle(new LoginQuery("admin", Password));

        Assert.True(result.IsSuccess);
        Assert.Equal($"token:{Admin.UserId}:admin:Admin", result.Value.AccessToken);
        Assert.Equal("Bearer", result.Value.TokenType);
        Assert.Equal(FakeTokenIssuer.ExpiresAt, result.Value.ExpiresAt);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        var result = await Handle(new LoginQuery("admin", "otra-contraseña"));

        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ReturnsTheSameErrorAndStillVerifiesAPassword()
    {
        var result = await Handle(new LoginQuery("nadie", Password));

        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal([null], _hasher.VerifiedHashes);
    }

    [Fact]
    public async Task HandleAsync_Rejected_LogsAWarningWithTheUsernameButNotThePassword()
    {
        await Handle(new LoginQuery("admin", "otra-contraseña"));

        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Contains("admin", record.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("otra-contraseña", record.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginQuery_ToString_DoesNotContainThePassword()
    {
        var text = new LoginQuery("admin", Password).ToString();

        Assert.Contains("admin", text, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, text, StringComparison.Ordinal);
    }

    private Task<Result<AuthToken>> Handle(LoginQuery query) =>
        new LoginHandler(_users, _hasher, new FakeTokenIssuer(), _logger).HandleAsync(query, CancellationToken.None);
}
