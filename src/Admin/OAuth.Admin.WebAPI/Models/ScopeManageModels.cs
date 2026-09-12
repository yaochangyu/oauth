namespace OAuth.Admin.WebAPI.Models;

public class ScopeDetailResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Resources { get; set; } = [];
    public bool IsSensitive { get; set; }
}

public class CreateScopeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<string> Resources { get; set; } = [];
    public bool IsSensitive { get; set; }
}

public class UpdateScopeRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<string> Resources { get; set; } = [];
    public bool IsSensitive { get; set; }
}
