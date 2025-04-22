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
    /// Data di scadenza del token
    /// </summary>
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>
    /// Indica se il token è stato revocato
    /// </summary>
    public bool IsRevoked { get; set; }
    
    /// <summary>
    /// Utente a cui appartiene il token
    /// </summary>
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}