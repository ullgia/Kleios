using FluentValidation;
using Kleios.Shared.Models;

namespace Kleios.Shared.Validators;

/// <summary>
/// Validatore per le richieste di creazione impostazione
/// </summary>
public class CreateSettingRequestValidator : AbstractValidator<CreateSettingRequest>
{
    public CreateSettingRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("La chiave dell'impostazione è obbligatoria")
            .Matches("^[a-zA-Z0-9\\-_.]+$").WithMessage("La chiave può contenere solo lettere, numeri, trattini, underscore e punti")
            .MaximumLength(100).WithMessage("La chiave non può superare i 100 caratteri");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Il valore è obbligatorio")
            .When(x => x.IsRequired);

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("La descrizione non può superare i 255 caratteri");

        RuleFor(x => x.DataType)
            .NotEmpty().WithMessage("Il tipo di dato è obbligatorio")
            .Must(BeValidDataType).WithMessage("Il tipo di dato deve essere uno dei seguenti: string, int, boolean, decimal, datetime, json");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("La categoria è obbligatoria")
            .MaximumLength(50).WithMessage("La categoria non può superare i 50 caratteri");
    }

    private bool BeValidDataType(string dataType)
    {
        var validTypes = new[]
        {
            "string", "int", "integer", "bool", "boolean", 
            "decimal", "number", "datetime", "date", "json"
        };
        
        return validTypes.Contains(dataType.ToLower());
    }
}

/// <summary>
/// Validatore per le richieste di aggiornamento impostazione
/// </summary>
public class UpdateSettingRequestValidator : AbstractValidator<UpdateSettingRequest>
{
    public UpdateSettingRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("La descrizione non può superare i 255 caratteri")
            .When(x => x.Description != null);
    }
}