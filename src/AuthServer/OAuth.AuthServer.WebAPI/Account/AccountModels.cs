namespace OAuth.AuthServer.WebAPI.Account;

public record RegisterRequest(string Email, string Password, string? DisplayName);

public record RegisterResponse(string UserId, string Email);

public record Failure(string Code, string Message);
