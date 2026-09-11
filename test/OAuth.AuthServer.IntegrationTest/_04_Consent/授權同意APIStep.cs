using FluentAssertions;
using Json.Path;
using Reqnroll;
using System.Text.Json.Nodes;

namespace OAuth.AuthServer.IntegrationTest._04_Consent;

[Binding]
public class 授權同意APIStep : Steps
{
    [Then(@"回傳內容中路徑 ""(.*)"" 應包含子字串 ""(.*)""")]
    public void Then回傳內容中路徑應包含子字串(string path, string expectedSubstring)
    {
        var jsonNode = (JsonNode?)this.ScenarioContext["JsonNode"];
        jsonNode.Should().NotBeNull();

        var result = JsonPath.Parse(path).Evaluate(jsonNode!);
        var value = result.Matches.FirstOrDefault()?.Value?.GetValue<string>();

        value.Should().NotBeNullOrEmpty();
        value!.Should().Contain(expectedSubstring);
    }
}
