using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OAuth.Admin.WebAPI.Services;
using Reqnroll;

namespace OAuth.Admin.WebAPI.IntegrationTest._03_ScopeAndAudit;

[Binding]
public class Scope與審計日誌管理Step : Steps
{
    [Given(@"系統中存在一筆審計日誌事件 ""(.*)"" 針對用戶 ""(.*)""")]
    public async Task Given系統中存在一筆審計日誌事件(string eventType, string actor)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

        await auditLogService.RecordAsync(new Models.AuditLogEntry
        {
            EventType = eventType,
            Actor = actor,
            Target = "app-violating",
            Details = "管理員強制停用違規應用",
            IpAddress = "127.0.0.1",
            Timestamp = DateTimeOffset.UtcNow,
        });
    }
}
