namespace Kleios.Shared.Models;

/// <summary>
/// Informazioni su una sessione utente attiva
/// </summary>
public class UserSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // Desktop, Mobile, Tablet
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // Città/Paese
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCurrentSession { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Richiesta per terminare una sessione
/// </summary>
public class TerminateSessionRequest
{
    public Guid SessionId { get; set; }
}

/// <summary>
/// Statistiche sulle sessioni utente
/// </summary>
public class SessionStatisticsDto
{
    public int TotalActiveSessions { get; set; }
    public int DesktopSessions { get; set; }
    public int MobileSessions { get; set; }
    public int TabletSessions { get; set; }
    public Dictionary<string, int> SessionsByBrowser { get; set; } = new();
    public Dictionary<string, int> SessionsByLocation { get; set; } = new();
    public DateTime? LastLoginTime { get; set; }
    public string? LastLoginIp { get; set; }
}

/// <summary>
/// Configurazione per la gestione delle sessioni
/// </summary>
public class SessionConfigurationDto
{
    public int SessionTimeoutMinutes { get; set; } = 60;
    public int MaxConcurrentSessions { get; set; } = 5;
    public bool AllowMultipleDevices { get; set; } = true;
    public bool NotifyOnNewLogin { get; set; } = true;
    public bool RequireReauthenticationForSensitiveActions { get; set; } = true;
    public int InactivityTimeoutMinutes { get; set; } = 30;
}
