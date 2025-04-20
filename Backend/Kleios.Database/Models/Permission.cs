using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Classe che rappresenta un permesso nel sistema
/// </summary>
public class Permission : BaseEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string SystemName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    // Relationship
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}