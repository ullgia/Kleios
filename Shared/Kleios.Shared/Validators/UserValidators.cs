using FluentValidation;
using Kleios.Shared.Models;

namespace Kleios.Shared.Validators;

/// <summary>
/// Validatore per le richieste di creazione utente
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Il nome utente è obbligatorio")
            .MinimumLength(3).WithMessage("Il nome utente deve contenere almeno 3 caratteri")
            .MaximumLength(50).WithMessage("Il nome utente non può superare i 50 caratteri");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email è obbligatoria")
            .EmailAddress().WithMessage("Il formato dell'email non è valido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La password è obbligatoria")
            .MinimumLength(8).WithMessage("La password deve contenere almeno 8 caratteri")
            .Matches("[A-Z]").WithMessage("La password deve contenere almeno una lettera maiuscola")
            .Matches("[a-z]").WithMessage("La password deve contenere almeno una lettera minuscola")
            .Matches("[0-9]").WithMessage("La password deve contenere almeno un numero")
            .Matches("[^a-zA-Z0-9]").WithMessage("La password deve contenere almeno un carattere speciale");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Il nome è obbligatorio")
            .MaximumLength(50).WithMessage("Il nome non può superare i 50 caratteri");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Il cognome è obbligatorio")
            .MaximumLength(50).WithMessage("Il cognome non può superare i 50 caratteri");
    }
}

/// <summary>
/// Validatore per le richieste di aggiornamento utente
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Il formato dell'email non è valido")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("La password deve contenere almeno 8 caratteri")
            .Matches("[A-Z]").WithMessage("La password deve contenere almeno una lettera maiuscola")
            .Matches("[a-z]").WithMessage("La password deve contenere almeno una lettera minuscola")
            .Matches("[0-9]").WithMessage("La password deve contenere almeno un numero")
            .Matches("[^a-zA-Z0-9]").WithMessage("La password deve contenere almeno un carattere speciale")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.FirstName)
            .MaximumLength(50).WithMessage("Il nome non può superare i 50 caratteri")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(50).WithMessage("Il cognome non può superare i 50 caratteri")
            .When(x => !string.IsNullOrEmpty(x.LastName));
    }
}