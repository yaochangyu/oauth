using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace OAuth.AuthServer.WebAPI.Infrastructure.Threads;

public class ThreadsAuthenticationOptions : OAuthOptions
{
    public ThreadsAuthenticationOptions()
    {
        ClaimsIssuer = ThreadsAuthenticationDefaults.Issuer;
        CallbackPath = ThreadsAuthenticationDefaults.CallbackPath;
        AuthorizationEndpoint = ThreadsAuthenticationDefaults.AuthorizationEndpoint;
        TokenEndpoint = ThreadsAuthenticationDefaults.TokenEndpoint;
        UserInformationEndpoint = ThreadsAuthenticationDefaults.UserInformationEndpoint;

        Scope.Add("threads_basic");

        ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.NameIdentifier, "id");
        ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Name, "username");
        ClaimActions.MapJsonKey("picture", "threads_profile_picture_url");
    }
}
