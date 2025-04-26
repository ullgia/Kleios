using Kleios.Shared.Attributes;

namespace Kleios.Shared.Settings;

/// <summary>
/// Modello principale delle impostazioni dell'applicazione
/// </summary>
public class AppSettingsModel
{
    /// <summary>
    /// Impostazioni per l'autenticazione JWT
    /// </summary>
    [Setting("E8562B25-A457-43FB-A24C-06C6159D46BB", "Jwt:Configuration", "Configurazione JWT", "Security")]
    public JwtSettingsModel Jwt { get; set; } = new();
    
    /// <summary>
    /// Impostazioni generali dell'applicazione
    /// </summary>
    [Setting("7A3D293F-3821-4E09-B431-7CDBA65C184A", "General:Configuration", "Impostazioni generali dell'applicazione", "General")]
    public GeneralSettingsModel General { get; set; } = new();
    
    /// <summary>
    /// Impostazioni per l'invio di email
    /// </summary>
    [Setting("B1C14515-8B6E-4B6C-BF0D-0B1ECAC78F1D", "Email:Configuration", "Configurazione email", "Communication")]
    public EmailSettingsModel Email { get; set; } = new();
}