using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Json.Path;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
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
    public static DeveloperTestFactory? DeveloperFactory { get; private set; }
    public static AccountTestFactory? AccountFactory { get; private set; }

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

        DeveloperFactory = new DeveloperTestFactory();
        AccountFactory = new AccountTestFactory();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        AccountFactory?.Dispose();
        DeveloperFactory?.Dispose();
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

    [Given(@"調用端以開發者身分 ""(.*)"" 呼叫 Developer WebAPI")]
    [When(@"調用端以開發者身分 ""(.*)"" 呼叫 Developer WebAPI")]
    public void Given調用端以開發者身分呼叫DeveloperWebAPI(string userId)
    {
        var client = DeveloperFactory!.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress       = new Uri("https://localhost/"),
        });
        this.ScenarioContext["HttpClient"] = client;

        var token = TestAssistant.GenerateTestJwtToken(userId);
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
    }

    [Given(@"調用端以使用者身分 ""(.*)"" 呼叫 Account WebAPI")]
    [When(@"調用端以使用者身分 ""(.*)"" 呼叫 Account WebAPI")]
    public void Given調用端以使用者身分呼叫AccountWebAPI(string userId)
    {
        var client = AccountFactory!.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress       = new Uri("https://localhost/"),
        });
        this.ScenarioContext["HttpClient"] = client;

        var token = TestAssistant.GenerateTestJwtToken(userId, role: "User");
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
    }

    [Given(@"以使用者 ""(.*)"" 的身分呼叫 Account WebAPI")]
    [When(@"以使用者 ""(.*)"" 的身分呼叫 Account WebAPI")]
    public async Task Given以使用者的身分呼叫AccountWebAPI(string userName)
    {
        using var scope = Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<OAuth.AuthServer.DB.ApplicationUser>>();
        var user = await userManager.FindByNameAsync(userName)
            ?? await userManager.FindByEmailAsync(userName)
            ?? throw new InvalidOperationException($"找不到使用者 {userName}");

        var client = AccountFactory!.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress       = new Uri("https://localhost/"),
        });
        this.ScenarioContext["HttpClient"] = client;

        var token = TestAssistant.GenerateTestJwtToken(user.Id, role: "admin");
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["CurrentUserId"] = user.Id;
    }

    [Given(@"調用端呼叫 AuthServer")]
    [When(@"調用端呼叫 AuthServer")]
    public void Given調用端呼叫AuthServer()
    {
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
                request.Headers.TryAddWithoutValidation(h.Key, ReplacePlaceholders(h.Value));
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

        this.ScenarioContext.Remove("Body");
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
        result.Matches.Should().NotBeEmpty($"路徑 '{jsonPath}' 未找到相符節點，原始 JSON: {responseBody}");

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

    [Then(@"從回應標頭 ""(.*)"" 擷取 query 參數 ""(.*)"" 儲存至 ""(.*)""")]
    public void Then從回應標頭擷取Query參數儲存至(string headerName, string queryParamName, string varName)
    {
        var response = (HttpResponseMessage)this.ScenarioContext["Response"];
        string? headerVal = null;
        if (response.Headers.TryGetValues(headerName, out var values))
            headerVal = values.FirstOrDefault();

        headerVal.Should().NotBeNull($"預期回應標頭包含 {headerName}");
        var uri = new Uri(headerVal!, UriKind.RelativeOrAbsolute);
        var queryStr = uri.IsAbsoluteUri ? uri.Query : (headerVal!.Contains('?') ? headerVal.Substring(headerVal.IndexOf('?')) : "");
        var parsed = HttpUtility.ParseQueryString(queryStr);
        var paramVal = parsed[queryParamName];

        paramVal.Should().NotBeNullOrEmpty($"標頭 {headerName} 中應包含 query 參數 {queryParamName}");
        this.ScenarioContext[varName] = paramVal;
    }

    [Then(@"回應標頭 ""(.*)"" 應包含 ""(.*)""")]
    public void Then回應標頭應包含(string headerName, string expectedValue)
    {
        var response = (HttpResponseMessage)this.ScenarioContext["Response"];
        string? actualValue = null;

        if (response.Headers.TryGetValues(headerName, out var values))
            actualValue = string.Join(", ", values);
        else if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
            actualValue = string.Join(", ", contentValues);

        actualValue.Should().NotBeNull($"預期回應標頭包含 {headerName}");
        actualValue.Should().Contain(expectedValue);
    }

    [Then(@"回應標頭 ""(.*)"" 不應包含 ""(.*)""")]
    public void Then回應標頭不應包含(string headerName, string unexpectedValue)
    {
        var response = (HttpResponseMessage)this.ScenarioContext["Response"];
        string? actualValue = null;

        if (response.Headers.TryGetValues(headerName, out var values))
            actualValue = string.Join(", ", values);
        else if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
            actualValue = string.Join(", ", contentValues);

        if (actualValue is not null)
        {
            actualValue.Should().NotContain(unexpectedValue);
        }
    }



    [Then(@"預期回傳內容中路徑 ""(.*)"" 包含字串清單")]
    public void Then預期回傳內容中路徑包含字串清單(string jsonPath, Table table)
    {
        var jsonNode = (JsonNode?)this.ScenarioContext["JsonNode"];
        jsonNode.Should().NotBeNull();

        var result = JsonPath.Parse(jsonPath).Evaluate(jsonNode!);
        result.Matches.Should().NotBeEmpty($"路徑 '{jsonPath}' 未找到相符節點");

        var node = result.Matches[0].Value;
        var array = node?.AsArray().Select(x => x?.ToString()).ToList();
        array.Should().NotBeNull();

        foreach (var row in table.Rows)
        {
            var expected = row[0];
            array.Should().Contain(expected);
        }
    }

    [Then(@"預期回傳內容中路徑 ""(.*)"" 為空或不存在")]
    public void Then預期回傳內容中路徑為空或不存在(string jsonPath)
    {
        var jsonNode = (JsonNode?)this.ScenarioContext["JsonNode"];
        if (jsonNode is null) return;

        var result = JsonPath.Parse(jsonPath).Evaluate(jsonNode);
        if (result.Matches.Count > 0)
        {
            var val = result.Matches[0].Value;
            val.Should().BeNull();
        }
    }

    public string ReplacePlaceholders(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        foreach (var key in this.ScenarioContext.Keys)
        {
            var val = this.ScenarioContext[key]?.ToString();
            if (val != null)
            {
                input = input.Replace($"{{{{{key}}}}}", val);
                if (key.StartsWith("Saved") || key.StartsWith("Created"))
                {
                    var shortKey = key.Replace("Saved", "").Replace("Created", "");
                    input = input.Replace($"{{{{{shortKey}}}}}", val);
                }
            }
        }
        return input;
    }
}
