using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using OAuth.AuthServer.DB;

namespace OAuth.AuthServer.WebAPI.Account;

public class AccountHandler(UserManager<ApplicationUser> userManager)
{
    public async Task<Result<RegisterResponse, Failure>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Result.Failure<RegisterResponse, Failure>(
                new Failure("email_already_exists", $"Email '{request.Email}' 已被使用"));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure<RegisterResponse, Failure>(
                new Failure(error.Code, error.Description));
        }

        return Result.Success<RegisterResponse, Failure>(
            new RegisterResponse(user.Id, user.Email!));
    }
}
