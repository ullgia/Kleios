using FluentValidation;
using Kleios.Shared.Models.Authentication;
using Kleios.Shared.Validators;

namespace Kleios.Shared.Validators.Authentication;

/// <summary>
/// Validatore per le richieste di login
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).Username();
        RuleFor(x => x.Password).NotEmpty().WithMessage("La password è obbligatoria");
        RuleFor(x => x.RememberMe).NotNull().WithMessage("Il campo 'Ricordami' è obbligatorio");
    }
}

/// <summary>
/// Validatore per le richieste di registrazione
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).Username();
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("La conferma della password è obbligatoria")
            .Equal(x => x.Password).WithMessage("La password e la conferma non corrispondono");
    }
}

/// <summary>
/// Validatore per le richieste di cambio password
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("La password attuale è obbligatoria");
        RuleFor(x => x.NewPassword).Password();
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("La conferma della password è obbligatoria")
            .Equal(x => x.NewPassword).WithMessage("La nuova password e la conferma non corrispondono");
    }
}