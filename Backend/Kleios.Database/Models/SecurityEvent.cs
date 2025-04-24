using System;
using System.ComponentModel.DataAnnotations;

namespace Kleios.Database.Models;

/// <summary>
/// Rappresenta un evento di sicurezza registrato nel sistema per finalità di audit e monitoraggio
/// </summary>
public class SecurityEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// ID dell'utente associato all'evento. Può essere vuoto per eventi di sistema
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Tipo di evento (es. "Login.Failed", "Token.Expired", "User.Created")
    /// </summary>
    [Required, MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrizione dettagliata dell'evento
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Data e ora dell'evento in UTC
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Severità dell'evento (Info, Warning, Error, Critical)
    /// </summary>
    [Required, MaxLength(50)]
    public string Severity { get; set; } = "Info";
    
    /// <summary>
    /// Riferimento all'utente associato all'evento
    /// </summary>
    public virtual ApplicationUser? User { get; set; }
}