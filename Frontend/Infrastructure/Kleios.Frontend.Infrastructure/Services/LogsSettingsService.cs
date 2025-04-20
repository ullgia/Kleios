// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\LogsSettingsService.cs
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Frontend.Infrastructure.Helpers;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio per la gestione dei logs e delle impostazioni di sistema
/// che utilizza service discovery di Aspire e HttpClientHelper
/// </summary>
public class LogsSettingsService : ILogsSettingsService
{
    private readonly HttpClient _httpClient;
    private const string LogsEndpoint = "api/logs";
    private const string SettingsEndpoint = "api/settings";

    public LogsSettingsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Ottiene i logs in base ai filtri specificati
    /// </summary>
    public async Task<Option<IEnumerable<LogEntry>>> GetLogsAsync(LogFilter filter)
    {
        // Costruzione parametri query string con il metodo helper ToQueryString
        return await _httpClient.Get<IEnumerable<LogEntry>>(LogsEndpoint, filter);
    }

    /// <summary>
    /// Ottiene le impostazioni di sistema
    /// </summary>
    public async Task<Option<SystemSettings>> GetSystemSettingsAsync()
    {
        return await _httpClient.Get<SystemSettings>(SettingsEndpoint);
    }

    /// <summary>
    /// Aggiorna le impostazioni di sistema
    /// </summary>
    public async Task<Option<SystemSettings>> UpdateSystemSettingsAsync(SystemSettings settings)
    {
        return await _httpClient.PutAsJson<SystemSettings>(SettingsEndpoint, settings);
    }
}