namespace OAuth.Account.WebAPI.Models;

public record AuthorizedAppDto(
    string AuthorizationId,
    string ClientId,
    string ClientDisplayName,
    List<string> Scopes,
    DateTime AuthorizedAt
);
