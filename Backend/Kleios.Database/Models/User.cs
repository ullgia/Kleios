using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Classe che rappresenta un utente del sistema
/// </summary>
[Obsolete("Utilizzare ApplicationUser al posto di User. Questa classe è mantenuta solo per compatibilità.")]
public class User : BaseEntity
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica se l'utente è il master con accesso completo e permessi immutabili
    /// </summary>
    public bool IsMasterUser { get; set; } = false;
}