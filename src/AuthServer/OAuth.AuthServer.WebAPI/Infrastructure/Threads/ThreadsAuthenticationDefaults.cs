namespace OAuth.AuthServer.WebAPI.Infrastructure.Threads;

public static class ThreadsAuthenticationDefaults
{
    public const string AuthenticationScheme = "Threads";
    public const string DisplayName = "Threads";
    public const string Issuer = "Threads";
    public const string CallbackPath = "/signin-threads";
    public const string AuthorizationEndpoint = "https://threads.net/oauth/authorize";
    public const string TokenEndpoint = "https://graph.threads.net/oauth/access_token";
    public const string UserInformationEndpoint = "https://graph.threads.net/v1.0/me?fields=id,username,threads_profile_picture_url";
}
