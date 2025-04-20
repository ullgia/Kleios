// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ILogsSettingsService.cs
using Kleios.Shared;
using Kleios.Shared.Models;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di gestione logs e impostazioni
/// </summary>
public interface ILogsSettingsService
{
    Task<Option<IEnumerable<LogEntry>>> GetLogsAsync(LogFilter filter);
    Task<Option<SystemSettings>> GetSystemSettingsAsync();
    Task<Option<SystemSettings>> UpdateSystemSettingsAsync(SystemSettings settings);
}