using FinanceSentry.Core.Exceptions;

namespace FinanceSentry.Modules.Auth.Domain.Exceptions;

public sealed class InvalidCredentialsException()
    : ApiException(401, "INVALID_CREDENTIALS", "Invalid email or password.");

public sealed class InvalidRefreshTokenException(string message = "Refresh token invalid or expired.")
    : ApiException(401, "INVALID_REFRESH_TOKEN", message);

public sealed class GoogleAccountOnlyException()
    : ApiException(401, "GOOGLE_ACCOUNT_ONLY", "This account uses Google sign-in. Please use 'Continue with Google'.");

public sealed class DuplicateEmailException()
    : ApiException(400, "DUPLICATE_EMAIL", "Email is already registered.");

public sealed class InvalidGoogleCredentialException()
    : ApiException(400, "INVALID_GOOGLE_CREDENTIAL", "Invalid Google credential.");
