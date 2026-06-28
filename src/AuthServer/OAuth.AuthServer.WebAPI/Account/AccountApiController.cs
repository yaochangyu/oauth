using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace OAuth.AuthServer.WebAPI.Account;

[ApiController]
[Route("api/v1/account")]
public class AccountApiController(AccountHandler handler, IValidator<RegisterRequest> validator) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await handler.RegisterAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Code == "email_already_exists")
                return Conflict(new { result.Error.Code, result.Error.Message });
            return BadRequest(new { result.Error.Code, result.Error.Message });
        }

        return CreatedAtAction(null, new { id = result.Value.UserId }, result.Value);
    }
}
