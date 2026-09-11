using Reqnroll;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;

namespace OAuth.AuthServer.IntegrationTest._05_AccountLogin;

[Binding]
public class 帳號登入登出Step : Steps
{
    [When(@"調用端連續發送 (\d+) 次 ""(.*)"" 請求至 ""(.*)""")]
    public async Task When調用端連續發送次請求至(int times, string method, string url)
    {
        var client = (HttpClient)this.ScenarioContext["HttpClient"];
        var httpMethod = new HttpMethod(method);

        for (var i = 0; i < times; i++)
        {
            using var request = new HttpRequestMessage(httpMethod, url);

            if (this.ScenarioContext.ContainsKey("Headers"))
            {
                var headers = (Dictionary<string, string>)this.ScenarioContext["Headers"];
                foreach (var h in headers)
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            if (this.ScenarioContext.ContainsKey("Body"))
            {
                var body = (string)this.ScenarioContext["Body"];
                request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);
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
}
