namespace OAuth.Account.WebAPI.Models;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

public record TwoFactorStatusResponse(
    bool IsTwoFactorEnabled,
    bool HasAuthenticator
);

public record Generate2FaKeyResponse(
    string SharedKey,
    string AuthenticatorUri
);

public record Verify2FaRequest(
    string Code
);

public record Disable2FaRequest(
    string? Password = null
);
