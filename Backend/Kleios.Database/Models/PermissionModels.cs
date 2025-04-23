using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    
    /// <summary>
    /// Gruppo logico a cui appartiene il permesso
    /// </summary>
    public string Group { get; set; } = string.Empty;
    
    // Relationship
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Classe che rappresenta la relazione molti-a-molti tra ruoli e permessi
/// </summary>
public class RolePermission
{
    [Key]
    public Guid RoleId { get; set; }
    
    [Key]
    public Guid PermissionId { get; set; }
    
    [ForeignKey("RoleId")]
    public virtual ApplicationRole Role { get; set; } = null!;
    
    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; } = null!;
}