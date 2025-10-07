namespace Kleios.Backend.Shared;

/// <summary>
/// Interfaccia per il servizio di invio email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Invia un'email di reset password
    /// </summary>
    /// <param name="email">Indirizzo email destinatario</param>
    /// <param name="userName">Nome dell'utente</param>
    /// <param name="resetToken">Token di reset password</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>True se l'email è stata inviata con successo</returns>
    Task<bool> SendPasswordResetEmailAsync(string email, string userName, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia un'email di conferma registrazione
    /// </summary>
    /// <param name="email">Indirizzo email destinatario</param>
    /// <param name="userName">Nome dell'utente</param>
    /// <param name="confirmationToken">Token di conferma</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>True se l'email è stata inviata con successo</returns>
    Task<bool> SendEmailConfirmationAsync(string email, string userName, string confirmationToken, CancellationToken cancellationToken = default);
}
