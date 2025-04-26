using Kleios.Shared.Attributes;

namespace Kleios.Shared.Settings;

/// <summary>
/// Impostazioni per l'autenticazione JWT
/// </summary>
[SettingGroup("Security", "Impostazioni di sicurezza e autenticazione")]
public class JwtSettingsModel
{
    /// <summary>
    /// Chiave segreta per la firma dei token JWT
    /// </summary>
    [Setting("F9DE812A-B4D6-4F2F-B403-7D8E6B9739E9", "Jwt:SecretKey", "Chiave segreta per la firma dei token JWT", "Security")]
    public string SecretKey { get; set; } = "your_default_super_secret_key_with_minimum_length_for_security";
    
    /// <summary>
    /// Emittente del token (chi ha generato il token)
    /// </summary>
    [Setting("E0A8D662-C8E3-4B31-A53C-1D6A68C6D1B1", "Jwt:Issuer", "Emittente del token (chi ha generato il token)", "Security")]
    public string Issuer { get; set; } = "KleiosAPI";
    
    /// <summary>
    /// Pubblico destinatario del token
    /// </summary>
    [Setting("1C76A31C-58C0-4E80-8B34-5845BD0EDADE", "Jwt:Audience", "Pubblico destinatario del token", "Security")]
    public string Audience { get; set; } = "KleiosClients";
    
    /// <summary>
    /// Durata di validità del token in minuti
    /// </summary>
    [Setting("D2B6722F-EE62-47FD-BFCE-35A5075C953F", "Jwt:TokenValidityInMinutes", "Durata di validità del token in minuti", "Security")]
    public int TokenValidityInMinutes { get; set; } = 60;
    
    /// <summary>
    /// Durata di validità del refresh token in giorni
    /// </summary>
    [Setting("3F98A15E-AB53-45B3-8A1D-45D23B28C7CE", "Jwt:RefreshTokenValidityInDays", "Durata di validità del refresh token in giorni", "Security")]
    public int RefreshTokenValidityInDays { get; set; } = 7;
}