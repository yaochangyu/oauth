using FluentAssertions;
using Reqnroll;
using System.Text.Json.Nodes;

namespace OAuth.AuthServer.IntegrationTest._03_Security;

[Binding]
public class 安全性驗證Step : Steps
{
    [Then(@"Discovery Document 包含 PKCE 支援聲明")]
    public void Then_Discovery_Document_包含PKCE支援聲明()
    {
        var jsonNode = (JsonNode?)this.ScenarioContext["JsonNode"];
        jsonNode.Should().NotBeNull();

        // 確認 code_challenge_methods_supported 包含 S256
        var methods = jsonNode!["code_challenge_methods_supported"]?.AsArray();
        methods.Should().NotBeNull("Discovery Document 應包含 code_challenge_methods_supported");
        methods!.Select(m => m?.GetValue<string>()).Should().Contain("S256", "應支援 PKCE S256");
    }
}
