namespace Kleios.Database.Models;

/// <summary>
/// Rappresenta un tentativo di accesso fallito
/// </summary>
public class FailedLoginAttempt : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime AttemptTime { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Rappresenta un indirizzo IP bloccato
/// </summary>
public class BlockedIp : BaseEntity
{
    public string IpAddress { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public bool IsPermanent { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
