using FluentValidation;

namespace OAuth.AuthServer.WebAPI.Account;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("密碼需包含至少一個大寫字母")
            .Matches("[a-z]").WithMessage("密碼需包含至少一個小寫字母")
            .Matches("[0-9]").WithMessage("密碼需包含至少一個數字");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .When(x => x.DisplayName is not null);
    }
}
