namespace OAuth.Client.Mvc.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public string? ErrorCode { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
