namespace OAuth.Admin.WebAPI.Models;

public class AppDetailResponse
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string ConsentType { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public string Status { get; set; } = "Sandbox"; // Sandbox | InReview | Approved | Rejected | Suspended
    public string? RejectReason { get; set; }
    public string? RequestReason { get; set; }
    public string? SubmittedAt { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public List<string> Requirements { get; set; } = [];
}

public class RejectAppRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CreateAppRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string ClientType { get; set; } = "confidential";
    public string ConsentType { get; set; } = "explicit";
    public string? Developer { get; set; }
    public string? Status { get; set; } = "Sandbox";
    public string? RequestReason { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public List<string> Requirements { get; set; } = [];
}

public class UpdateAppRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string ClientType { get; set; } = "confidential";
    public string ConsentType { get; set; } = "explicit";
    public string? Developer { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public List<string> Requirements { get; set; } = [];
}
