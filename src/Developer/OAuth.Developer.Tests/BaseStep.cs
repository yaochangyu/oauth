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

namespace OAuth.Developer.Tests;

[Binding]
[CollectionDefinition("OAuth.Developer.Tests", DisableParallelization = true)]
public class BaseStep : Steps
{
    private static readonly List<IContainer> TestContainers = [];
    public static DeveloperTestFactory? Factory { get; private set; }

    private const string 字串等於 = "字串等於";
    private const string 數值等於 = "數值等於";
    private const string 布林值等於 = "布林值等於";
    private const string 包含字串 = "包含字串";
    private const string 不為空 = "不為空";

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        var postgres = await TestAssistant.CreatePostgresContainerAsync();
        TestContainers.Add(postgres);
        TestAssistant.SetDbConnectionEnvironmentVariable(postgres.GetConnectionString());

        Factory = new DeveloperTestFactory();
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
        var client = Factory!.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress       = new Uri("https://localhost/"),
        });
        this.ScenarioContext["HttpClient"] = client;
    }

    [Given(@"調用端已使用開發者身分 ""(.*)"" 取得有效 JWT Token")]
    [When(@"調用端已使用開發者身分 ""(.*)"" 取得有效 JWT Token")]
    public void Given調用端已使用開發者身分取得有效JWTToken(string userId)
    {
        var token = TestAssistant.GenerateTestJwtToken(userId);
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["CurrentDeveloperUserId"] = userId;
    }

    [Given(@"調用端未帶入任何認證 Token")]
    [When(@"調用端未帶入任何認證 Token")]
    public void Given調用端未帶入任何認證Token()
    {
        if (this.ScenarioContext.ContainsKey("Headers"))
        {
            var headers = (Dictionary<string, string>)this.ScenarioContext["Headers"];
            headers.Remove("Authorization");
            this.ScenarioContext["Headers"] = headers;
        }
    }

    [Given(@"調用端使用無效的 JWT Token")]
    [When(@"調用端使用無效的 JWT Token")]
    public void Given調用端使用無效的JWTToken()
    {
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = "Bearer invalid_token_xyz";
        this.ScenarioContext["Headers"] = headers;
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
    {
        json = ReplacePlaceholders(json);
        this.ScenarioContext["Body"] = json;
    }

    [When(@"調用端發送 ""(.*)"" 請求至 ""(.*)""")]
    public async Task When調用端發送請求至(string method, string url)
    {
        url = ReplacePlaceholders(url);

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

        // Clear one-time request data
        this.ScenarioContext.Remove("Body");
    }

    [Then(@"調用端應收到 HTTP 狀態碼為 ""(.*)""")]
    public void Then調用端應收到HTTP狀態碼為(int expectedStatusCode)
    {
        var response = (HttpResponseMessage)this.ScenarioContext["Response"];
        var responseBody = (string)this.ScenarioContext["ResponseBody"];
        ((int)response.StatusCode).Should().Be(expectedStatusCode, $"Response body was: {responseBody}");
    }

    [Then(@"回應內容驗證")]
    public void Then回應內容驗證(Table table)
    {
        var responseBody = (string)this.ScenarioContext["ResponseBody"];
        var jsonNode = JsonNode.Parse(responseBody);
        jsonNode.Should().NotBeNull();

        foreach (var row in table.Rows)
        {
            var jsonPath = row["欄位路徑"];
            var op = row["驗證方式"];
            var expected = row["預期值"];

            var path = JsonPath.Parse(jsonPath);
            var result = path.Evaluate(jsonNode);
            result.Matches.Should().NotBeEmpty($"路徑 '{jsonPath}' 未找到相符節點，原始 JSON: {responseBody}");

            var actualNode = result.Matches[0].Value;

            switch (op)
            {
                case 字串等於:
                    actualNode?.GetValue<string>().Should().Be(expected);
                    break;
                case 數值等於:
                    actualNode?.GetValue<long>().Should().Be(long.Parse(expected));
                    break;
                case 布林值等於:
                    actualNode?.GetValue<bool>().Should().Be(bool.Parse(expected));
                    break;
                case 包含字串:
                    actualNode?.ToString().Should().Contain(expected);
                    break;
                case 不為空:
                    actualNode?.ToString().Should().NotBeNullOrWhiteSpace();
                    break;
                default:
                    throw new NotSupportedException($"不支援的驗證方式：{op}");
            }
        }
    }

    [Given(@"從回應中儲存變數 ""(.*)"" 為 JSON 欄位 ""(.*)""")]
    [When(@"從回應中儲存變數 ""(.*)"" 為 JSON 欄位 ""(.*)""")]
    [Then(@"從回應中儲存變數 ""(.*)"" 為 JSON 欄位 ""(.*)""")]
    public void When從回應中儲存變數為JSON欄位(string variableName, string jsonPath)
    {
        var responseBody = (string)this.ScenarioContext["ResponseBody"];
        var jsonNode = JsonNode.Parse(responseBody);
        jsonNode.Should().NotBeNull();

        var path = JsonPath.Parse(jsonPath);
        var result = path.Evaluate(jsonNode);
        result.Matches.Should().NotBeEmpty($"路徑 '{jsonPath}' 未找到相符節點");

        var node = result.Matches[0].Value;
        string? val = null;
        if (node is JsonValue jsonVal)
        {
            if (jsonVal.TryGetValue<string>(out var strVal))
                val = strVal;
            else
                val = jsonVal.ToString();
        }
        else
        {
            val = node?.ToString();
        }
        this.ScenarioContext[variableName] = val;
    }

    private string ReplacePlaceholders(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        foreach (var key in this.ScenarioContext.Keys)
        {
            var val = this.ScenarioContext[key]?.ToString();
            if (val != null)
            {
                input = input.Replace($"{{{{{key}}}}}", val);
                if (key.StartsWith("Saved"))
                {
                    var shortKey = key["Saved".Length..];
                    input = input.Replace($"{{{{{shortKey}}}}}", val);
                }
            }
        }
        return input;
    }
}
