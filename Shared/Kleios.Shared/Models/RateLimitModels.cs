namespace Kleios.Shared.Models;

/// <summary>
/// Configurazione per il rate limiting
/// </summary>
public class RateLimitConfigurationDto
{
    public bool EnableRateLimiting { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public bool EnableIpBlocking { get; set; } = true;
    public int BlockDurationMinutes { get; set; } = 60;
    public int SuspiciousActivityThreshold { get; set; } = 10;
}

/// <summary>
/// Statistiche sui tentativi di accesso falliti
/// </summary>
public class LoginAttemptsStatisticsDto
{
    public int TotalFailedAttempts { get; set; }
    public int UniqueIpAddresses { get; set; }
    public int BlockedIpAddresses { get; set; }
    public List<FailedLoginAttemptDto> RecentAttempts { get; set; } = new();
    public Dictionary<string, int> AttemptsByIp { get; set; } = new();
}

/// <summary>
/// Dettagli di un tentativo di accesso fallito
/// </summary>
public class FailedLoginAttemptDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime AttemptTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Informazioni su un IP bloccato
/// </summary>
public class BlockedIpDto
{
    public Guid Id { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public bool IsPermanent { get; set; }
}

/// <summary>
/// Richiesta per bloccare un IP
/// </summary>
public class BlockIpRequest
{
    public string IpAddress { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; } // null = permanente
}

/// <summary>
/// Richiesta per sbloccare un IP
/// </summary>
public class UnblockIpRequest
{
    public string IpAddress { get; set; } = string.Empty;
}
