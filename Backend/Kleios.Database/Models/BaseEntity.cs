using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Classe base per tutte le entità del database
/// </summary>
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}