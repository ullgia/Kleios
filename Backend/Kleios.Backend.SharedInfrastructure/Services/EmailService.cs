using Kleios.Backend.Shared;
using Microsoft.Extensions.Logging;

namespace Kleios.Backend.SharedInfrastructure.Services;

/// <summary>
/// Implementazione del servizio email
/// Nota: Questa è un'implementazione stub che logga le email.
/// Per produzione, implementare con SMTP (MailKit) o servizio cloud (SendGrid, AWS SES, Azure Communication Services)
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(
        string email, 
        string userName, 
        string resetToken, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO PRODUZIONE: Implementare con MailKit (SMTP) o SendGrid/AWS SES
            // Esempio con MailKit:
            // using var message = new MimeMessage();
            // message.From.Add(new MailboxAddress("Kleios", "noreply@kleios.com"));
            // message.To.Add(new MailboxAddress(userName, email));
            // message.Subject = "Reset Password - Kleios";
            // message.Body = new TextPart("html") { Text = GetPasswordResetHtml(userName, resetToken) };
            // 
            // using var client = new SmtpClient();
            // await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            // await client.AuthenticateAsync(_smtpUser, _smtpPassword, cancellationToken);
            // await client.SendAsync(message, cancellationToken);
            // await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "EMAIL: Password Reset per {Email} (User: {UserName}). Token: {Token}", 
                email, userName, resetToken);
            
            _logger.LogInformation(
                "Per resettare la password, usa questo link: https://localhost:5000/auth/Account/ResetPassword?email={Email}&token={Token}",
                email, System.Net.WebUtility.UrlEncode(resetToken));

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio dell'email di reset password a {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendEmailConfirmationAsync(
        string email, 
        string userName, 
        string confirmationToken, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "EMAIL: Conferma Email per {Email} (User: {UserName}). Token: {Token}", 
                email, userName, confirmationToken);
            
            _logger.LogInformation(
                "Per confermare l'email, usa questo link: https://localhost:5000/auth/Account/ConfirmEmail?email={Email}&token={Token}",
                email, System.Net.WebUtility.UrlEncode(confirmationToken));

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio dell'email di conferma a {Email}", email);
            return false;
        }
    }

    // Template HTML per email (base)
    private static string GetPasswordResetHtml(string userName, string resetToken)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reset Password - Kleios</title>
</head>
<body style='font-family: Arial, sans-serif; padding: 20px;'>
    <h2>Reset Password</h2>
    <p>Ciao {userName},</p>
    <p>Hai richiesto il reset della tua password. Clicca sul link seguente per procedere:</p>
    <p><a href='https://localhost:5000/auth/Account/ResetPassword?token={System.Net.WebUtility.UrlEncode(resetToken)}'>Reset Password</a></p>
    <p>Se non hai richiesto il reset, ignora questa email.</p>
    <p>Il link scadrà tra 24 ore.</p>
    <hr>
    <p style='color: #666; font-size: 12px;'>Kleios - Sistema di Autenticazione</p>
</body>
</html>";
    }
}
