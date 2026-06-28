using Microsoft.AspNetCore.Authentication;

namespace OAuth.AuthServer.WebAPI.Infrastructure.Threads;

public static class ThreadsAuthenticationExtensions
{
    public static AuthenticationBuilder AddThreads(
        this AuthenticationBuilder builder,
        Action<ThreadsAuthenticationOptions> configureOptions)
        => builder.AddOAuth<ThreadsAuthenticationOptions, ThreadsAuthenticationHandler>(
            ThreadsAuthenticationDefaults.AuthenticationScheme,
            ThreadsAuthenticationDefaults.DisplayName,
            configureOptions);
}
