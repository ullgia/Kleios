using Microsoft.AspNetCore.Identity;

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