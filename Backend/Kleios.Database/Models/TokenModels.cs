using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kleios.Database.Models;

/// <summary>
/// Classe che rappresenta un refresh token per l'autenticazione
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// ID dell'utente a cui appartiene il token
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Token utilizzato per il refresh dell'accesso
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// ID del JWT associato a questo refresh token
    /// </summary>
    public string JwtId { get; set; } = string.Empty;
    
    /// <summary>
    /// Data di scadenza del token
    /// </summary>
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>
    /// Indica se il token è stato revocato
    /// </summary>
    public bool IsRevoked { get; set; }
    
    /// <summary>
    /// Data in cui il token è stato revocato
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Motivo della revoca (utile per tracking di sicurezza)
    /// </summary>
    public string? RevocationReason { get; set; }
    
    /// <summary>
    /// Indirizzo IP utilizzato per la creazione del token
    /// </summary>
    public string CreatedByIp { get; set; } = string.Empty;
    
    /// <summary>
    /// Indirizzo IP dell'ultimo utilizzo
    /// </summary>
    public string? LastUsedByIp { get; set; }
    
    /// <summary>
    /// User Agent del browser/client
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Data dell'ultimo utilizzo
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Contatore utilizzi (utile per rilevare utilizzi anomali)
    /// </summary>
    public int UseCount { get; set; } = 0;
    
    /// <summary>
    /// Utente a cui appartiene il token
    /// </summary>
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}

/// <summary>
/// Classe che rappresenta un token utente per l'autenticazione frontend
/// </summary>
public class UserToken : BaseEntity
{
    /// <summary>
    /// ID dell'utente a cui appartiene il token
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Tipo di token (access o refresh)
    /// </summary>
    [Required]
    public string TokenType { get; set; } = string.Empty;
    
    /// <summary>
    /// Valore del token
    /// </summary>
    [Required]
    public string TokenValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Data di scadenza del token
    /// </summary>
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>
    /// Identificativo del dispositivo/browser
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Indirizzo IP dell'ultimo utilizzo
    /// </summary>
    public string LastIpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica se il token è stato revocato
    /// </summary>
    public bool IsRevoked { get; set; } = false;
    
    /// <summary>
    /// Utente a cui appartiene il token
    /// </summary>
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}