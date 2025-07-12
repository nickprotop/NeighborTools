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
    string? Country = null,
    bool AcceptTerms = false,
    bool AcceptPrivacyPolicy = false,
    bool AcceptDataProcessing = false,
    bool AcceptMarketing = false
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

public record ConfirmEmailCommand(
    string UserId,
    string Token
);

public record ResendEmailVerificationCommand(
    string Email
);

public class AuthResult
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public bool EmailVerificationRequired { get; set; } = false;
}