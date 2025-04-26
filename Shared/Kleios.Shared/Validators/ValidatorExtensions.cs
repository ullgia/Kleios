using FluentValidation;

namespace Kleios.Shared.Validators;

/// <summary>
/// Classe di estensione per funzionalità comuni di validazione
/// </summary>
public static class ValidatorExtensions
{
    /// <summary>
    /// Validazione per le password con requisiti standard
    /// </summary>
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("La password è obbligatoria")
            .MinimumLength(8).WithMessage("La password deve contenere almeno 8 caratteri")
            .Matches("[A-Z]").WithMessage("La password deve contenere almeno una lettera maiuscola")
            .Matches("[a-z]").WithMessage("La password deve contenere almeno una lettera minuscola")
            .Matches("[0-9]").WithMessage("La password deve contenere almeno un numero")
            .Matches("[^a-zA-Z0-9]").WithMessage("La password deve contenere almeno un carattere speciale");
    }

    /// <summary>
    /// Validazione per le email
    /// </summary>
    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("L'email è obbligatoria")
            .EmailAddress().WithMessage("Formato email non valido");
    }
    
    /// <summary>
    /// Validazione per i nomi utente
    /// </summary>
    public static IRuleBuilderOptions<T, string> Username<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Il nome utente è obbligatorio")
            .MinimumLength(3).WithMessage("Il nome utente deve contenere almeno 3 caratteri")
            .MaximumLength(50).WithMessage("Il nome utente non può superare i 50 caratteri")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Il nome utente può contenere solo lettere, numeri, punti, trattini e underscore");
    }
}