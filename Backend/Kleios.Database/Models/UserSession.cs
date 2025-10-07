using Kleios.Database.Models;

namespace Kleios.Database.Models;

/// <summary>
/// Rappresenta una sessione utente attiva
/// </summary>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string SessionToken { get; set; } = string.Empty;
    
    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
