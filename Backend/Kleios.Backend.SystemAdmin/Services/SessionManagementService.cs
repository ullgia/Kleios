using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared.Models;
using Kleios.Backend.Shared;
using Microsoft.EntityFrameworkCore;
using UAParser;

namespace Kleios.Backend.SystemAdmin.Services;

public interface ISessionManagementService
{
    Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId);
    Task<UserSessionDto> CreateSessionAsync(Guid userId, string ipAddress, string userAgent);
    Task<bool> TerminateSessionAsync(Guid sessionId, Guid requestingUserId);
    Task<bool> TerminateAllSessionsAsync(Guid userId, Guid? exceptSessionId = null);
    Task<bool> UpdateSessionActivityAsync(Guid sessionId);
    Task CleanupExpiredSessionsAsync();
    Task<SessionStatisticsDto> GetSessionStatisticsAsync(Guid userId);
    Task<SessionConfigurationDto> GetSessionConfigurationAsync();
    Task<SessionConfigurationDto> UpdateSessionConfigurationAsync(SessionConfigurationDto config);
}

public class SessionManagementService : ISessionManagementService
{
    private readonly KleiosDbContext _context;
    private readonly ILogger<SessionManagementService> _logger;
    private readonly ISettingsService _settingsService;

    public SessionManagementService(
        KleiosDbContext context,
        ILogger<SessionManagementService> logger,
        ISettingsService settingsService)
    {
        _context = context;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivity)
            .ToListAsync();

        return sessions.Select(s => MapToDto(s, false)).ToList();
    }

    public async Task<UserSessionDto> CreateSessionAsync(Guid userId, string ipAddress, string userAgent)
    {
        // Parse user agent
        var parser = Parser.GetDefault();
        var clientInfo = parser.Parse(userAgent);

        var config = await GetSessionConfigurationAsync();

        // Controlla il numero massimo di sessioni
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .CountAsync();

        if (activeSessions >= config.MaxConcurrentSessions)
        {
            // Termina la sessione più vecchia
            var oldestSession = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderBy(s => s.LastActivity)
                .FirstOrDefaultAsync();

            if (oldestSession != null)
            {
                oldestSession.IsActive = false;
                _logger.LogInformation("Sessione {SessionId} terminata per raggiunto limite massimo", oldestSession.Id);
            }
        }

        var session = new UserSession
        {
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceType = GetDeviceType(clientInfo),
            Browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}",
            OperatingSystem = $"{clientInfo.OS.Family} {clientInfo.OS.Major}",
            Location = await GetLocationFromIpAsync(ipAddress),
            ExpiresAt = DateTime.UtcNow.AddMinutes(config.SessionTimeoutMinutes),
            SessionToken = Guid.NewGuid().ToString("N")
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nuova sessione creata per l'utente {UserId} da {IpAddress}", userId, ipAddress);

        return MapToDto(session, false);
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId, Guid requestingUserId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        
        if (session == null)
        {
            _logger.LogWarning("Tentativo di terminare sessione non esistente: {SessionId}", sessionId);
            return false;
        }

        // Verifica che l'utente possa terminare questa sessione
        if (session.UserId != requestingUserId)
        {
            _logger.LogWarning("Utente {UserId} ha tentato di terminare sessione di altro utente", requestingUserId);
            return false;
        }

        session.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sessione {SessionId} terminata dall'utente {UserId}", sessionId, requestingUserId);
        return true;
    }

    public async Task<bool> TerminateAllSessionsAsync(Guid userId, Guid? exceptSessionId = null)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        // Se exceptSessionId è fornito, è il JwtId (claim "jti") della sessione corrente
        if (exceptSessionId.HasValue)
        {
            // Filtra per JwtId se presente, altrimenti per Id (fallback)
            sessions = sessions.Where(s => 
                (s.JwtId.HasValue && s.JwtId.Value != exceptSessionId.Value) || 
                (!s.JwtId.HasValue && s.Id != exceptSessionId.Value)
            ).ToList();
            
            _logger.LogDebug("Esclusa sessione corrente con JwtId={JwtId} dalla terminazione", exceptSessionId);
        }

        foreach (var session in sessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Terminate {Count} sessioni per l'utente {UserId}", sessions.Count, userId);
        return true;
    }

    public async Task<bool> UpdateSessionActivityAsync(Guid sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        
        if (session == null || !session.IsActive)
        {
            return false;
        }

        session.LastActivity = DateTime.UtcNow;
        
        var config = await GetSessionConfigurationAsync();
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(config.SessionTimeoutMinutes);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _context.UserSessions
            .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pulite {Count} sessioni scadute", expiredSessions.Count);
    }

    public async Task<SessionStatisticsDto> GetSessionStatisticsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        var lastLogin = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        return new SessionStatisticsDto
        {
            TotalActiveSessions = sessions.Count,
            DesktopSessions = sessions.Count(s => s.DeviceType == "Desktop"),
            MobileSessions = sessions.Count(s => s.DeviceType == "Mobile"),
            TabletSessions = sessions.Count(s => s.DeviceType == "Tablet"),
            SessionsByBrowser = sessions.GroupBy(s => s.Browser)
                .ToDictionary(g => g.Key, g => g.Count()),
            SessionsByLocation = sessions.GroupBy(s => s.Location)
                .ToDictionary(g => g.Key, g => g.Count()),
            LastLoginTime = lastLogin?.CreatedAt,
            LastLoginIp = lastLogin?.IpAddress
        };
    }

    public async Task<SessionConfigurationDto> GetSessionConfigurationAsync()
    {
        return new SessionConfigurationDto
        {
            SessionTimeoutMinutes = await _settingsService.GetSettingValueAsync<int?>("Security:Session:TimeoutMinutes") ?? 60,
            MaxConcurrentSessions = await _settingsService.GetSettingValueAsync<int?>("Security:Session:MaxConcurrentSessions") ?? 5,
            AllowMultipleDevices = await _settingsService.GetSettingValueAsync<bool?>("Security:Session:AllowMultipleDevices") ?? true,
            NotifyOnNewLogin = await _settingsService.GetSettingValueAsync<bool?>("Security:Session:NotifyOnNewLogin") ?? true,
            RequireReauthenticationForSensitiveActions = await _settingsService.GetSettingValueAsync<bool?>("Security:Session:RequireReauthenticationForSensitiveActions") ?? true,
            InactivityTimeoutMinutes = await _settingsService.GetSettingValueAsync<int?>("Security:Session:InactivityTimeoutMinutes") ?? 30
        };
    }

    public async Task<SessionConfigurationDto> UpdateSessionConfigurationAsync(SessionConfigurationDto config)
    {
        await _settingsService.UpdateSettingAsync("Security:Session:TimeoutMinutes", config.SessionTimeoutMinutes.ToString());
        await _settingsService.UpdateSettingAsync("Security:Session:MaxConcurrentSessions", config.MaxConcurrentSessions.ToString());
        await _settingsService.UpdateSettingAsync("Security:Session:AllowMultipleDevices", config.AllowMultipleDevices.ToString());
        await _settingsService.UpdateSettingAsync("Security:Session:NotifyOnNewLogin", config.NotifyOnNewLogin.ToString());
        await _settingsService.UpdateSettingAsync("Security:Session:RequireReauthenticationForSensitiveActions", config.RequireReauthenticationForSensitiveActions.ToString());
        await _settingsService.UpdateSettingAsync("Security:Session:InactivityTimeoutMinutes", config.InactivityTimeoutMinutes.ToString());

        _logger.LogInformation("Configurazione sessioni aggiornata");
        return config;
    }

    private UserSessionDto MapToDto(UserSession session, bool isCurrentSession)
    {
        return new UserSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Username = session.User?.UserName ?? string.Empty,
            IpAddress = session.IpAddress,
            UserAgent = session.UserAgent,
            DeviceType = session.DeviceType,
            Browser = session.Browser,
            OperatingSystem = session.OperatingSystem,
            Location = session.Location,
            CreatedAt = session.CreatedAt,
            LastActivity = session.LastActivity,
            ExpiresAt = session.ExpiresAt,
            IsCurrentSession = isCurrentSession,
            IsActive = session.IsActive
        };
    }

    private string GetDeviceType(ClientInfo clientInfo)
    {
        if (clientInfo.Device.IsSpider) return "Bot";
        if (clientInfo.Device.Family.Contains("Mobile")) return "Mobile";
        if (clientInfo.Device.Family.Contains("Tablet")) return "Tablet";
        return "Desktop";
    }

    private async Task<string> GetLocationFromIpAsync(string ipAddress)
    {
        // Implementazione geolocalizzazione IP con fallback sicuro
        try
        {
            // Per produzione considera: ip-api.com (free 45 req/min), ipapi.co, ipgeolocation.io
            // Per ora implementiamo con ip-api.com (free tier)
            
            // Verifica IP valido (no localhost/private)
            if (string.IsNullOrEmpty(ipAddress) || 
                ipAddress == "::1" || 
                ipAddress.StartsWith("127.") || 
                ipAddress.StartsWith("192.168.") ||
                ipAddress.StartsWith("10.") ||
                ipAddress.StartsWith("172."))
            {
                return "Local Network";
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await httpClient.GetStringAsync($"http://ip-api.com/json/{ipAddress}?fields=status,country,regionName,city");
            
            var json = System.Text.Json.JsonDocument.Parse(response);
            if (json.RootElement.GetProperty("status").GetString() == "success")
            {
                var country = json.RootElement.GetProperty("country").GetString();
                var region = json.RootElement.GetProperty("regionName").GetString();
                var city = json.RootElement.GetProperty("city").GetString();
                return $"{city}, {region}, {country}";
            }
            
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore durante la geolocalizzazione dell'IP {IpAddress}", ipAddress);
            return "Unknown";
        }
    }
}
