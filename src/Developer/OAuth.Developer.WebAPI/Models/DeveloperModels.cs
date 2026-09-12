using System.Text.Json.Serialization;

namespace OAuth.Developer.WebAPI.Models;

public static class DeveloperAppConstants
{
    public const string TypeWeb = "Web";
    public const string TypeSpa = "SPA";
    public const string TypeMobile = "Mobile";

    public const string StatusSandbox = "Sandbox";
    public const string StatusInReview = "InReview";
    public const string StatusApproved = "Approved";
    public const string StatusRejected = "Rejected";

    public const string PropDeveloperUserId = "developer_user_id";
    public const string PropAppType = "app_type";
    public const string PropDescription = "description";
    public const string PropLogoUrl = "logo_url";
    public const string PropStatus = "status";
    public const string PropReviewSubmittedAt = "review_submitted_at";
    public const string PropReviewNotes = "review_notes";
    public const string PropCreatedAt = "created_at";
    public const string PropRetiringSecretHash = "retiring_secret_hash";
    public const string PropRetiringSecretExpiresAt = "retiring_secret_expires_at";
}

public class CreateAppRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string AppType { get; set; } = DeveloperAppConstants.TypeWeb;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> RequestedScopes { get; set; } = [];
}

public class UpdateAppRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public List<string>? RedirectUris { get; set; }
    public List<string>? PostLogoutRedirectUris { get; set; }
    public List<string>? RequestedScopes { get; set; }
}

public class AppResponse
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AppType { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = DeveloperAppConstants.StatusSandbox;
    public bool RequiresPkce { get; set; } = true;
    public string? ClientSecret { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> RequestedScopes { get; set; } = [];
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ReviewSubmittedAt { get; set; }
}

public class CredentialsResponse
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public bool HasActiveSecret { get; set; }
    public string? ActiveSecretMasked { get; set; }
    public bool HasRetiringSecret { get; set; }
    public DateTimeOffset? RetiringSecretExpiresAt { get; set; }
    public double? RetiringSecretExpiresInSeconds { get; set; }
}

public class RotateSecretResponse
{
    public string ClientId { get; set; } = string.Empty;
    public string NewSecret { get; set; } = string.Empty;
    public DateTimeOffset RetiringSecretExpiresAt { get; set; }
    public string Message { get; set; } = "金鑰輪替成功，舊金鑰已進入過渡緩衝期（7 天內依然有效）。";
}

public class RevokeRetiringSecretResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "舊金鑰已成功立即作廢。";
}

public class SubmitReviewRequest
{
    public string? Notes { get; set; }
}

public class ReviewStatusResponse
{
    public string AppId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SubmittedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public class EnableDeveloperRequest
{
    public string? OrganizationName { get; set; }
    public string? ContactEmail { get; set; }
    public bool AcceptAgreement { get; set; }
}

public class DeveloperStatusResponse
{
    public bool IsDeveloperEnabled { get; set; }
    public string? OrganizationName { get; set; }
    public string? ContactEmail { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }
}

public class GenerateAuthorizeUrlRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string? Scope { get; set; } = "openid profile email";
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } = "S256";
    public string? State { get; set; }
    public string? Prompt { get; set; }
}

public class GenerateAuthorizeUrlResponse
{
    public string AuthorizeUrl { get; set; } = string.Empty;
}

public class ValidateCredentialsRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class ValidateCredentialsResponse
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}
