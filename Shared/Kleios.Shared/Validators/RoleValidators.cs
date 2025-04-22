using FluentValidation;
using Kleios.Shared.Models;

namespace Kleios.Shared.Validators;

/// <summary>
/// Validatore per le richieste di creazione ruolo
/// </summary>
public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Il nome del ruolo è obbligatorio")
            .MinimumLength(3).WithMessage("Il nome del ruolo deve contenere almeno 3 caratteri")
            .MaximumLength(50).WithMessage("Il nome del ruolo non può superare i 50 caratteri");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("La descrizione non può superare i 255 caratteri")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

/// <summary>
/// Validatore per le richieste di aggiornamento ruolo
/// </summary>
public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("La descrizione non può superare i 255 caratteri")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}