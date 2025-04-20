using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kleios.Database.Models;

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