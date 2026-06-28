namespace OAuth.Admin.WebUI.Requests
{
    public class GetApplicationsRequest : PagedDataRequest
    {
        public string ApplicationFilter { get; set; } = string.Empty;
    }
}
