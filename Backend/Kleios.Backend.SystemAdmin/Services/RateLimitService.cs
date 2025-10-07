using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared.Models;
using Kleios.Backend.Shared;
using Microsoft.EntityFrameworkCore;

namespace Kleios.Backend.SystemAdmin.Services;

public interface IRateLimitService
{
    Task<bool> IsIpBlockedAsync(string ipAddress);
    Task<bool> RecordFailedLoginAttemptAsync(string username, string ipAddress, string userAgent, string reason);
    Task<bool> BlockIpAsync(BlockIpRequest request);
    Task<bool> UnblockIpAsync(string ipAddress);
    Task<IEnumerable<BlockedIpDto>> GetBlockedIpsAsync();
    Task<LoginAttemptsStatisticsDto> GetLoginAttemptsStatisticsAsync();
    Task<RateLimitConfigurationDto> GetRateLimitConfigurationAsync();
    Task<RateLimitConfigurationDto> UpdateRateLimitConfigurationAsync(RateLimitConfigurationDto config);
    Task CleanupOldAttemptsAsync();
}

public class RateLimitService : IRateLimitService
{
    private readonly KleiosDbContext _context;
    private readonly ILogger<RateLimitService> _logger;
    private readonly ISettingsService _settingsService;

    public RateLimitService(
        KleiosDbContext context,
        ILogger<RateLimitService> logger,
        ISettingsService settingsService)
    {
        _context = context;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        var config = await GetRateLimitConfigurationAsync();
        
        if (!config.EnableIpBlocking)
        {
            return false;
        }

        var blockedIp = await _context.BlockedIps
            .FirstOrDefaultAsync(b => b.IpAddress == ipAddress && b.IsActive);

        if (blockedIp == null)
        {
            return false;
        }

        // Se è un blocco permanente
        if (blockedIp.IsPermanent)
        {
            return true;
        }

        // Se il blocco è scaduto
        if (blockedIp.ExpiresAt.HasValue && blockedIp.ExpiresAt.Value < DateTime.UtcNow)
        {
            blockedIp.IsActive = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Blocco IP scaduto per {IpAddress}", ipAddress);
            return false;
        }

        return true;
    }

    public async Task<bool> RecordFailedLoginAttemptAsync(string username, string ipAddress, string userAgent, string reason)
    {
        var attempt = new FailedLoginAttempt
        {
            Username = username,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Reason = reason
        };

        _context.FailedLoginAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Tentativo di login fallito per {Username} da {IpAddress}: {Reason}", 
            username, ipAddress, reason);

        // Controlla se l'IP deve essere bloccato
        await CheckAndBlockSuspiciousIpAsync(ipAddress);

        return true;
    }

    private async Task CheckAndBlockSuspiciousIpAsync(string ipAddress)
    {
        var config = await GetRateLimitConfigurationAsync();
        
        if (!config.EnableIpBlocking)
        {
            return;
        }

        var recentAttempts = await _context.FailedLoginAttempts
            .Where(a => a.IpAddress == ipAddress && a.AttemptTime > DateTime.UtcNow.AddMinutes(-config.BlockDurationMinutes))
            .CountAsync();

        if (recentAttempts >= config.SuspiciousActivityThreshold)
        {
            var existingBlock = await _context.BlockedIps
                .FirstOrDefaultAsync(b => b.IpAddress == ipAddress && b.IsActive);

            if (existingBlock == null)
            {
                var blockedIp = new BlockedIp
                {
                    IpAddress = ipAddress,
                    Reason = $"Superato limite di {config.SuspiciousActivityThreshold} tentativi falliti",
                    FailedAttempts = recentAttempts,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(config.BlockDurationMinutes),
                    IsPermanent = false
                };

                _context.BlockedIps.Add(blockedIp);
                await _context.SaveChangesAsync();

                _logger.LogWarning("IP {IpAddress} bloccato automaticamente per attività sospetta ({Attempts} tentativi)", 
                    ipAddress, recentAttempts);
            }
        }
    }

    public async Task<bool> BlockIpAsync(BlockIpRequest request)
    {
        var existingBlock = await _context.BlockedIps
            .FirstOrDefaultAsync(b => b.IpAddress == request.IpAddress && b.IsActive);

        if (existingBlock != null)
        {
            _logger.LogWarning("IP {IpAddress} è già bloccato", request.IpAddress);
            return false;
        }

        var blockedIp = new BlockedIp
        {
            IpAddress = request.IpAddress,
            Reason = request.Reason,
            IsPermanent = !request.DurationMinutes.HasValue,
            ExpiresAt = request.DurationMinutes.HasValue 
                ? DateTime.UtcNow.AddMinutes(request.DurationMinutes.Value) 
                : null
        };

        _context.BlockedIps.Add(blockedIp);
        await _context.SaveChangesAsync();

        _logger.LogInformation("IP {IpAddress} bloccato manualmente: {Reason}", request.IpAddress, request.Reason);
        return true;
    }

    public async Task<bool> UnblockIpAsync(string ipAddress)
    {
        var blockedIp = await _context.BlockedIps
            .FirstOrDefaultAsync(b => b.IpAddress == ipAddress && b.IsActive);

        if (blockedIp == null)
        {
            _logger.LogWarning("Tentativo di sbloccare IP non bloccato: {IpAddress}", ipAddress);
            return false;
        }

        blockedIp.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("IP {IpAddress} sbloccato", ipAddress);
        return true;
    }

    public async Task<IEnumerable<BlockedIpDto>> GetBlockedIpsAsync()
    {
        var blockedIps = await _context.BlockedIps
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.BlockedAt)
            .ToListAsync();

        return blockedIps.Select(b => new BlockedIpDto
        {
            Id = b.Id,
            IpAddress = b.IpAddress,
            BlockedAt = b.BlockedAt,
            ExpiresAt = b.ExpiresAt ?? DateTime.MaxValue,
            Reason = b.Reason,
            FailedAttempts = b.FailedAttempts,
            IsPermanent = b.IsPermanent
        });
    }

    public async Task<LoginAttemptsStatisticsDto> GetLoginAttemptsStatisticsAsync()
    {
        var since = DateTime.UtcNow.AddDays(-7);
        
        var attempts = await _context.FailedLoginAttempts
            .Where(a => a.AttemptTime >= since)
            .ToListAsync();

        var recentAttempts = attempts
            .OrderByDescending(a => a.AttemptTime)
            .Take(50)
            .Select(a => new FailedLoginAttemptDto
            {
                Id = a.Id,
                Username = a.Username,
                IpAddress = a.IpAddress,
                AttemptTime = a.AttemptTime,
                Reason = a.Reason,
                UserAgent = a.UserAgent
            })
            .ToList();

        var attemptsByIp = attempts
            .GroupBy(a => a.IpAddress)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        var blockedCount = await _context.BlockedIps
            .CountAsync(b => b.IsActive);

        return new LoginAttemptsStatisticsDto
        {
            TotalFailedAttempts = attempts.Count,
            UniqueIpAddresses = attempts.Select(a => a.IpAddress).Distinct().Count(),
            BlockedIpAddresses = blockedCount,
            RecentAttempts = recentAttempts,
            AttemptsByIp = attemptsByIp
        };
    }

    public async Task<RateLimitConfigurationDto> GetRateLimitConfigurationAsync()
    {
        return new RateLimitConfigurationDto
        {
            EnableRateLimiting = await _settingsService.GetSettingValueAsync<bool?>("Security:RateLimit:Enabled") ?? true,
            RequestsPerMinute = await _settingsService.GetSettingValueAsync<int?>("Security:RateLimit:RequestsPerMinute") ?? 60,
            RequestsPerHour = await _settingsService.GetSettingValueAsync<int?>("Security:RateLimit:RequestsPerHour") ?? 1000,
            EnableIpBlocking = await _settingsService.GetSettingValueAsync<bool?>("Security:IpBlocking:Enabled") ?? true,
            BlockDurationMinutes = await _settingsService.GetSettingValueAsync<int?>("Security:IpBlocking:DurationMinutes") ?? 60,
            SuspiciousActivityThreshold = await _settingsService.GetSettingValueAsync<int?>("Security:IpBlocking:SuspiciousThreshold") ?? 10
        };
    }

    public async Task<RateLimitConfigurationDto> UpdateRateLimitConfigurationAsync(RateLimitConfigurationDto config)
    {
        await _settingsService.UpdateSettingAsync("Security:RateLimit:Enabled", config.EnableRateLimiting.ToString());
        await _settingsService.UpdateSettingAsync("Security:RateLimit:RequestsPerMinute", config.RequestsPerMinute.ToString());
        await _settingsService.UpdateSettingAsync("Security:RateLimit:RequestsPerHour", config.RequestsPerHour.ToString());
        await _settingsService.UpdateSettingAsync("Security:IpBlocking:Enabled", config.EnableIpBlocking.ToString());
        await _settingsService.UpdateSettingAsync("Security:IpBlocking:DurationMinutes", config.BlockDurationMinutes.ToString());
        await _settingsService.UpdateSettingAsync("Security:IpBlocking:SuspiciousThreshold", config.SuspiciousActivityThreshold.ToString());

        _logger.LogInformation("Configurazione rate limiting aggiornata");
        return config;
    }

    public async Task CleanupOldAttemptsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        var oldAttempts = await _context.FailedLoginAttempts
            .Where(a => a.AttemptTime < cutoffDate)
            .ToListAsync();

        _context.FailedLoginAttempts.RemoveRange(oldAttempts);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rimossi {Count} tentativi falliti vecchi", oldAttempts.Count);
    }
}
