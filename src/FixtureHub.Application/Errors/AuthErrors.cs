using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Errors;

public static class AuthErrors
{
    public static readonly Error UsernameRequired =
        Error.Validation("Auth.UsernameRequired", "El usuario es obligatorio.");

    public static readonly Error PasswordRequired =
        Error.Validation("Auth.PasswordRequired", "La contraseña es obligatoria.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "El usuario o la contraseña no son correctos.");
}
