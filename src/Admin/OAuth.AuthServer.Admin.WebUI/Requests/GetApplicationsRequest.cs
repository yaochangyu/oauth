namespace OAuth.AuthServer.Admin.WebUI.Requests
{
    public class GetApplicationsRequest : PagedDataRequest
    {
        public string ApplicationFilter { get; set; } = string.Empty;
    }
}
