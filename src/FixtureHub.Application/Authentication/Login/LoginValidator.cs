using FixtureHub.Application.Errors;
using FluentValidation;

namespace FixtureHub.Application.Authentication.Login;

internal sealed class LoginValidator : AbstractValidator<LoginQuery>
{
    public LoginValidator()
    {
        RuleFor(query => query.Username)
            .NotEmpty()
            .WithErrorCode(AuthErrors.UsernameRequired.Code);

        RuleFor(query => query.Password)
            .NotEmpty()
            .WithErrorCode(AuthErrors.PasswordRequired.Code);
    }
}
