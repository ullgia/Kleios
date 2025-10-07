using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Modello per i log di audit delle operazioni del sistema
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// ID dell'utente che ha eseguito l'operazione
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Username dell'utente
    /// </summary>
    [MaxLength(256)]
    public string? Username { get; set; }
    
    /// <summary>
    /// Tipo di azione eseguita (Create, Update, Delete, View, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo di risorsa su cui è stata eseguita l'azione (User, Role, Setting, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID della risorsa interessata
    /// </summary>
    [MaxLength(256)]
    public string? ResourceId { get; set; }
    
    /// <summary>
    /// Descrizione dell'operazione
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Dati prima della modifica (JSON)
    /// </summary>
    public string? OldValues { get; set; }
    
    /// <summary>
    /// Dati dopo la modifica (JSON)
    /// </summary>
    public string? NewValues { get; set; }
    
    /// <summary>
    /// Indirizzo IP da cui è stata eseguita l'operazione
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User Agent del browser/client
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Timestamp dell'operazione
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Risultato dell'operazione (Success, Failed, etc.)
    /// </summary>
    [MaxLength(20)]
    public string Result { get; set; } = "Success";
    
    /// <summary>
    /// Messaggio di errore se l'operazione è fallita
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}
