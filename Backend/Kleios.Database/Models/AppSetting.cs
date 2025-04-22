using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Rappresenta un'impostazione dell'applicazione
/// </summary>
public class AppSetting
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public required string Key { get; set; }
    
    public string? Value { get; set; }
    
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string DataType { get; set; } = "string";
    
    public bool IsRequired { get; set; } = false;
    
    public bool IsReadOnly { get; set; } = false;
    
    [MaxLength(50)]
    public string Category { get; set; } = "General";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}