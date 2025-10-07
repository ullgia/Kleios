using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Classe che rappresenta un utente del sistema basata su IdentityUser
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica se l'utente è il master con accesso completo e permessi immutabili
    /// </summary>
    public bool IsMasterUser { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Data dell'ultimo cambio password
    /// </summary>
    public DateTime? LastPasswordChangeDate { get; set; }
    
    /// <summary>
    /// Indica se l'utente deve cambiare la password al prossimo accesso
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Collezione di refresh token associati all'utente
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

/// <summary>
/// Classe che rappresenta un ruolo di sistema basata su IdentityRole
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
    
    public bool IsSystemRole { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Classe che rappresenta un claim utente personalizzato
/// </summary>
public class ApplicationUserClaim : IdentityUserClaim<Guid>
{
    /// <summary>
    /// Data di creazione del claim
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data di ultimo aggiornamento del claim
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Utente associato al claim
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;
}

/// <summary>
/// Classe che rappresenta l'associazione tra un utente e un ruolo
/// </summary>
public class ApplicationUserRole : IdentityUserRole<Guid>
{
    /// <summary>
    /// Data di assegnazione del ruolo all'utente
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data di ultimo aggiornamento dell'associazione
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Utente associato
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;
    
    /// <summary>
    /// Ruolo associato
    /// </summary>
    public virtual ApplicationRole Role { get; set; } = null!;
}

/// <summary>
/// Classe che rappresenta un login utente esterno (es. Google, Facebook)
/// </summary>
public class ApplicationUserLogin : IdentityUserLogin<Guid>
{
    /// <summary>
    /// Data di creazione del login
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data di ultimo accesso con questo provider
    /// </summary>
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Indirizzo IP dell'ultimo accesso
    /// </summary>
    [MaxLength(50)]
    public string LastIpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Utente associato
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;
}

/// <summary>
/// Classe che rappresenta un claim associato a un ruolo
/// </summary>
public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
    /// <summary>
    /// Data di creazione del claim
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data di ultimo aggiornamento del claim
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Ruolo associato
    /// </summary>
    public virtual ApplicationRole Role { get; set; } = null!;
}

/// <summary>
/// Classe che rappresenta un token utente di Identity personalizzato
/// </summary>
public class ApplicationUserToken : IdentityUserToken<Guid>
{
    /// <summary>
    /// Data di creazione del token
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data di scadenza del token (se applicabile)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Indica se il token è stato utilizzato
    /// </summary>
    public bool IsUsed { get; set; } = false;
    
    /// <summary>
    /// Indica se il token è stato revocato
    /// </summary>
    public bool IsRevoked { get; set; } = false;
    
    /// <summary>
    /// Indirizzo IP che ha richiesto il token
    /// </summary>
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// User agent del browser/dispositivo che ha richiesto il token
    /// </summary>
    [MaxLength(512)]
    public string UserAgent { get; set; } = string.Empty;
    
    /// <summary>
    /// Utente associato
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;
}