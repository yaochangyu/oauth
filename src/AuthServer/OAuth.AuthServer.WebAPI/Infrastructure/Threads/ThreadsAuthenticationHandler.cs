using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OAuth.AuthServer.WebAPI.Infrastructure.Threads;

public class ThreadsAuthenticationHandler(
    IOptionsMonitor<ThreadsAuthenticationOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder)
    : OAuthHandler<ThreadsAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticationTicket> CreateTicketAsync(
        System.Security.Claims.ClaimsIdentity identity,
        AuthenticationProperties properties,
        OAuthTokenResponse tokens)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using var response = await Backchannel.SendAsync(request, Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Threads userinfo 請求失敗：{response.StatusCode}");

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var context = new OAuthCreatingTicketContext(
            new System.Security.Claims.ClaimsPrincipal(identity),
            properties,
            Context,
            Scheme,
            Options,
            Backchannel,
            tokens,
            payload.RootElement);

        context.RunClaimActions();
        await Events.CreatingTicket(context);

        return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
    }
}
