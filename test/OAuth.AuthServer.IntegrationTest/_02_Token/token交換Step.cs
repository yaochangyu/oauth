using FluentAssertions;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Text;

namespace OAuth.AuthServer.IntegrationTest._02_Token;

[Binding]
public class token交換Step : Steps
{
    [Given(@"調用端已準備 Body 參數\(Form\)")]
    public void Given調用端已準備BodyForm(string formBody)
        => this.ScenarioContext["FormBody"] = formBody;

    [When(@"調用端發送 Form ""(.*)"" 請求至 ""(.*)""")]
    public async Task When調用端發送Form請求至(string method, string url)
    {
        var client = (HttpClient)this.ScenarioContext["HttpClient"];
        var httpMethod = new HttpMethod(method);
        using var request = new HttpRequestMessage(httpMethod, url);

        var formBody = (string)this.ScenarioContext["FormBody"];
        request.Content = new StringContent(formBody, Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        this.ScenarioContext["Response"] = response;
        this.ScenarioContext["ResponseBody"] = responseBody;
        this.ScenarioContext["StatusCode"] = (int)response.StatusCode;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(responseBody);
                this.ScenarioContext["JsonNode"] = node;
            }
            catch { /* 非 JSON */ }
        }
    }

    [Then(@"回傳的 Access Token 格式為 JWT")]
    public void Then回傳的AccessToken格式為JWT()
    {
        var jsonNode = (System.Text.Json.Nodes.JsonNode?)this.ScenarioContext["JsonNode"];
        jsonNode.Should().NotBeNull();

        var accessToken = jsonNode!["access_token"]?.GetValue<string>();
        accessToken.Should().NotBeNullOrEmpty("應包含 access_token");

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(accessToken).Should().BeTrue("Access Token 應為有效 JWT 格式");
    }
}
