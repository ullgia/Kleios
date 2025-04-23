using System;

namespace Kleios.Frontend.Shared.Models;

/// <summary>
/// Rappresenta le informazioni di un token di autenticazione
/// </summary>
public class TokenInfo
{
    /// <summary>
    /// Valore del token
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Data di scadenza del token
    /// </summary>
    public DateTime Expiry { get; set; }
    
    /// <summary>
    /// Tipo di token (access o refresh)
    /// </summary>
    public string TokenType { get; set; } = string.Empty;
    
    /// <summary>
    /// Identificativo del dispositivo/browser
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Data di creazione del token
    /// </summary>
    public DateTime CreatedAt { get; set; }
}