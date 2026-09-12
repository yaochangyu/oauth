namespace OAuth.Account.WebAPI.Models;

public record UserProfileResponse(
    string UserId,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    bool EmailConfirmed,
    bool IsTwoFactorEnabled
);

public record UpdateProfileRequest(
    string? DisplayName,
    string? AvatarUrl
);
