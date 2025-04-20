// filepath: c:\Users\Giacomo\source\Kleios\Shared\Kleios.Shared\Models\LogsModels.cs
namespace Kleios.Shared.Models;

/// <summary>
/// Modello per il filtro dei logs
/// </summary>
public class LogFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Level { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Modello per una voce di log
/// </summary>
public class LogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
    public string? UserName { get; set; }
    public string? Source { get; set; }
}

/// <summary>
/// Modello per le impostazioni di sistema
/// </summary>
public class SystemSettings
{
    public Guid Id { get; set; }
    public string ApplicationName { get; set; } = "Kleios";
    public string LogLevel { get; set; } = "Information";
    public int LogRetentionDays { get; set; } = 30;
    public bool EnableNotifications { get; set; } = true;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string EmailSender { get; set; } = string.Empty;
}