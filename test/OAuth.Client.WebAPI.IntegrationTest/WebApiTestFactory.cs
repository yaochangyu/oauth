using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OAuth.Client.WebAPI.IntegrationTest;

public class WebApiTestFactory : WebApplicationFactory<Program>
{
    public HttpClient WebApiClient => CreateClient();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
    }
}
