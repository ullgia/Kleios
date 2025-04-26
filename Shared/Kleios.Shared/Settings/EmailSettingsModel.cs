using Kleios.Shared.Attributes;

namespace Kleios.Shared.Settings;

/// <summary>
/// Impostazioni per l'invio di email
/// </summary>
[SettingGroup("Communication", "Impostazioni di comunicazione e notifiche")]
public class EmailSettingsModel
{
    /// <summary>
    /// Server SMTP per l'invio di email
    /// </summary>
    [Setting("A58DE9F2-1B3C-4C39-B9AB-D21C5E2D6451", "Email:SmtpServer", "Server SMTP per l'invio di email", "Communication")]
    public string SmtpServer { get; set; } = "smtp.example.com";
    
    /// <summary>
    /// Porta del server SMTP
    /// </summary>
    [Setting("7D3B941E-C5A0-4E67-B6D8-8F0A25F11C30", "Email:SmtpPort", "Porta del server SMTP", "Communication")]
    public int SmtpPort { get; set; } = 587;
    
    /// <summary>
    /// Nome utente per l'autenticazione SMTP
    /// </summary>
    [Setting("1E6CA897-53D5-4F32-83CE-7CB8DB47F3E5", "Email:SmtpUsername", "Nome utente per l'autenticazione SMTP", "Communication")]
    public string SmtpUsername { get; set; } = "";
    
    /// <summary>
    /// Password per l'autenticazione SMTP
    /// </summary>
    [Setting("DF0E425C-3172-41DB-937C-107643A5BCD5", "Email:SmtpPassword", "Password per l'autenticazione SMTP", "Communication", IsMasked = true, IsEncrypted = true)]
    public string SmtpPassword { get; set; } = "";
    
    /// <summary>
    /// Indica se utilizzare SSL per la connessione SMTP
    /// </summary>
    [Setting("6D7F82A0-3B9E-47E8-8B31-2AEB49F81C89", "Email:EnableSsl", "Abilita SSL per la connessione SMTP", "Communication")]
    public bool EnableSsl { get; set; } = true;
    
    /// <summary>
    /// Indirizzo email mittente predefinito
    /// </summary>
    [Setting("C3E91A6F-5F72-4D32-A925-D9C2F42D6586", "Email:DefaultSenderEmail", "Indirizzo email mittente predefinito", "Communication")]
    public string DefaultSenderEmail { get; set; } = "noreply@kleios.com";
    
    /// <summary>
    /// Nome mittente predefinito
    /// </summary>
    [Setting("9F742E35-D62A-47B5-9E8C-62F3BF9B04A8", "Email:DefaultSenderName", "Nome mittente predefinito", "Communication")]
    public string DefaultSenderName { get; set; } = "Kleios System";
}