using FixtureHub.Application.Authentication.Login;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Authentication.Login;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_UsernameAndPassword_HasNoErrors()
    {
        var result = _validator.TestValidate(new LoginQuery("admin", "contraseña-de-prueba"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_BlankUsernameAndPassword_ReturnsTheRequiredCodes(string? value)
    {
        var result = _validator.TestValidate(new LoginQuery(value!, value!));

        result.ShouldHaveValidationErrorFor(query => query.Username).WithErrorCode("Auth.UsernameRequired");
        result.ShouldHaveValidationErrorFor(query => query.Password).WithErrorCode("Auth.PasswordRequired");
    }
}
