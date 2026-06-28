using System.Runtime.Serialization;

namespace OAuth.Admin.WebUI.Requests
{
    [DataContract]
    public class GetScopesRequest : PagedDataRequest
    {
       public string ScopesFilter { get; set; } = string.Empty;
    }
}
