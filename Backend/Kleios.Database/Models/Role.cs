using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Classe che rappresenta un ruolo nel sistema
/// </summary>
[Obsolete("Utilizzare ApplicationRole al posto di Role. Questa classe è mantenuta solo per compatibilità.")]
public class Role : BaseEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica se il ruolo è predefinito e non può essere modificato
    /// </summary>
    public bool IsSystemRole { get; set; } = false;
    
    // Relationship
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}