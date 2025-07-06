namespace ToolsSharing.Core.Features.Auth;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateTime DateOfBirth,
    string? Address = null,
    string? City = null,
    string? PostalCode = null,
    string? Country = null
);

public record LoginCommand(
    string Email,
    string Password
);

public record RefreshTokenCommand(
    string RefreshToken
);

public record ForgotPasswordCommand(
    string Email
);

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
);

public record AuthResult(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);