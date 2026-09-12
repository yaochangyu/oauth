using Testcontainers.PostgreSql;

namespace OAuth.Developer.Tests;

public static class TestAssistant
{
    public static async Task<PostgreSqlContainer> CreatePostgresContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("oauth_developer_test")
            .WithUsername("oauth")
            .WithPassword("oauth_pass")
            .Build();

        await container.StartAsync();
        return container;
    }

    public static void SetDbConnectionEnvironmentVariable(string connectionString)
        => Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
}
