using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kleios.Database.Models;

/// <summary>
/// Classe che rappresenta la relazione molti-a-molti tra utenti e ruoli
/// </summary>
[Obsolete("Questa classe è stata sostituita da IdentityUserRole<Guid> di ASP.NET Core Identity. Non utilizzare più questa classe.")]
public class UserRole
{
    [Key]
    public Guid UserId { get; set; }
    
    [Key]
    public Guid RoleId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}