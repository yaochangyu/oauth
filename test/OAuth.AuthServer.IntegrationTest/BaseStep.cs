using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Json.Path;
using Reqnroll;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OAuth.AuthServer.IntegrationTest;

[Binding]
[CollectionDefinition("OAuth.AuthServer.IntegrationTest", DisableParallelization = true)]
public class BaseStep : Steps
{
    private static readonly List<IContainer> TestContainers = [];
    public static AuthServerTestFactory? Factory { get; private set; }

    private const string 字串等於 = "字串等於";
    private const string 數值等於 = "數值等於";
    private const string 布林值等於 = "布林值等於";

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        var postgres = await TestAssistant.CreatePostgresContainerAsync();
        TestContainers.Add(postgres);
        TestAssistant.SetDbConnectionEnvironmentVariable(postgres.GetConnectionString());

        Factory = new AuthServerTestFactory();
        await Factory.InitializeDatabaseAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        Factory?.Dispose();
        for (var i = TestContainers.Count - 1; i >= 0; i--)
            await TestContainers[i].StopAsync();
        TestContainers.Clear();
    }

    [Given(@"初始化測試伺服器")]
    public void Given初始化測試伺服器()
    {
        // BaseAddress 設為 https:// 讓 OpenIddict 看到 HTTPS scheme（TestServer 不做真正的 TLS）
        var client = Factory!.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress       = new Uri("https://localhost/"),
        });
        this.ScenarioContext["HttpClient"] = client;
    }

    [Given(@"調用端已準備 Header 參數")]
    public void Given調用端已準備Header參數(Table table)
    {
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        foreach (var row in table.Rows)
            foreach (var header in table.Header)
                headers[header] = row[header];

        this.ScenarioContext["Headers"] = headers;
    }

    [Given(@"調用端已準備 Body 參數\(Json\)")]
    public void Given調用端已準備BodyJson(string json)
        => this.ScenarioContext["Body"] = json;

    [When(@"調用端發送 ""(.*)"" 請求至 ""(.*)""")]
    public async Task When調用端發送請求至(string method, string url)
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

    [Then(@"預期得到 HttpStatusCode 為 ""(.*)""")]
    public void Then預期得到HttpStatusCode為(int expected)
    {
        var actual = (int)this.ScenarioContext["StatusCode"];
        actual.Should().Be(expected);
    }

    [Then(@"預期回傳內容為")]
    public void Then預期回傳內容為(string expected)
    {
        var actual = (string)this.ScenarioContext["ResponseBody"];
        var actualNode = JsonNode.Parse(actual);
        var expectedNode = JsonNode.Parse(expected);
        JsonNode.DeepEquals(actualNode, expectedNode).Should().BeTrue(
            $"預期：{expected}\n實際：{actual}");
    }

    [Then(@"預期回傳內容中路徑 ""(.*)"" 的""(.*)"" ""(.*)""")]
    public void Then預期回傳內容中路徑的(string path, string type, string expected)
    {
        var jsonNode = (JsonNode?)this.ScenarioContext["JsonNode"];
        jsonNode.Should().NotBeNull();

        var result = JsonPath.Parse(path).Evaluate(jsonNode!);
        var value = result.Matches.FirstOrDefault()?.Value;

        switch (type)
        {
            case 字串等於:
                value?.GetValue<string>().Should().Be(expected);
                break;
            case 數值等於:
                value?.GetValue<int>().Should().Be(int.Parse(expected));
                break;
            case 布林值等於:
                value?.GetValue<bool>().Should().Be(bool.Parse(expected));
                break;
        }
    }
}
