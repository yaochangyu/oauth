using Reqnroll;
using System.Text.Json.Nodes;

namespace OAuth.Admin.WebAPI.IntegrationTest._00_Authorization;

[Binding]
public class 管理員權限保護Step : Steps
{
    [Given(@"使用者已登入為一般用戶 ""(.*)"" 角色為 ""(.*)""")]
    [Given(@"使用者已登入為管理員 ""(.*)"" 角色為 ""(.*)""")]
    public void Given使用者已登入為特定角色(string username, string role)
    {
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["X-Test-User"] = username;
        headers["X-Test-Role"] = role;
        this.ScenarioContext["Headers"] = headers;
    }

    [When(@"未登入使用者發送 ""(.*)"" 請求至 ""(.*)""")]
    public async Task When未登入使用者發送請求至(string method, string url)
    {
        var client = (HttpClient)this.ScenarioContext["HttpClient"];
        var httpMethod = new HttpMethod(method);
        using var request = new HttpRequestMessage(httpMethod, url);

        // 未登入請求：不附加任何身分識別 Header
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        this.ScenarioContext["Response"] = response;
        this.ScenarioContext["ResponseBody"] = responseBody;
        this.ScenarioContext["StatusCode"] = (int)response.StatusCode;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try { this.ScenarioContext["JsonNode"] = JsonNode.Parse(responseBody); }
            catch { /* 非 JSON 回應 */ }
        }
    }

    [When(@"使用者發送 ""(.*)"" 請求至 ""(.*)""")]
    public async Task When使用者發送請求至(string method, string url)
    {
        var client = (HttpClient)this.ScenarioContext["HttpClient"];
        var httpMethod = new HttpMethod(method);
        using var request = new HttpRequestMessage(httpMethod, url);

        if (this.ScenarioContext.ContainsKey("Headers"))
        {
            var headers = (Dictionary<string, string>)this.ScenarioContext["Headers"];
            foreach (var h in headers)
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        this.ScenarioContext["Response"] = response;
        this.ScenarioContext["ResponseBody"] = responseBody;
        this.ScenarioContext["StatusCode"] = (int)response.StatusCode;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try { this.ScenarioContext["JsonNode"] = JsonNode.Parse(responseBody); }
            catch { /* 非 JSON 回應 */ }
        }
    }
}
