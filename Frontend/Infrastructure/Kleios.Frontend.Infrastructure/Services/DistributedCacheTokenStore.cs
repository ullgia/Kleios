// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\DistributedCacheTokenStore.cs
using System.Security.Cryptography;
using System.Text;
using Kleios.Frontend.Shared.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione di ITokenStore che utilizza IDistributedCache per salvare e recuperare i token
/// </summary>
public class DistributedCacheTokenStore : ITokenStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheTokenStore> _logger;
    
    // Una mappa di contesti (es. circuiti) ai token ID
    private static readonly Dictionary<string, string> _contextTokenMap = new();
    
    // Per gestire la concorrenza nella mappa
    private static readonly SemaphoreSlim _mapLock = new(1, 1);
    
    private const string TokenIdKeyPrefix = "user_token_id_";
    private const string JwtTokenKeyPrefix = "jwt_";
    private const string RefreshTokenKeyPrefix = "refresh_";
    
    public DistributedCacheTokenStore(
        IDistributedCache cache,
        ILogger<DistributedCacheTokenStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    /// <summary>
    /// Genera un ID per il token che sarà usato come chiave per la cache distribuita
    /// </summary>
    private string GenerateTokenId(Guid userId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("KleiosTokenSalt"));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{userId}_{DateTime.UtcNow.Ticks}"));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
    
    /// <summary>
    /// Ottiene l'ID del token per un utente specifico, eventualmente creandone uno nuovo
    /// </summary>
    private async Task<string> GetOrCreateTokenIdAsync(Guid userId, string? context = null)
    {
        string? tokenId = null;
        
        // Se abbiamo un contesto, proviamo a recuperare l'ID dalla mappa
        if (!string.IsNullOrEmpty(context))
        {
            await _mapLock.WaitAsync();
            try
            {
                if (_contextTokenMap.TryGetValue(context, out var id))
                {
                    tokenId = id;
                    _logger.LogDebug("Token ID recuperato dalla mappa di contesto: {TokenId}", tokenId);
                }
            }
            finally
            {
                _mapLock.Release();
            }
        }
        
        // Se non abbiamo trovato l'ID nella mappa, proviamo a recuperarlo dalla cache
        if (string.IsNullOrEmpty(tokenId))
        {
            tokenId = await _cache.GetStringAsync($"{TokenIdKeyPrefix}{userId}");
            _logger.LogDebug("Token ID recuperato dalla cache: {TokenId}", tokenId);
        }
        
        // Se non abbiamo trovato l'ID nella cache, ne generiamo uno nuovo
        if (string.IsNullOrEmpty(tokenId))
        {
            tokenId = GenerateTokenId(userId);
            _logger.LogInformation("Generato nuovo Token ID: {TokenId}", tokenId);
            
            // Salviamo l'ID nella cache
            await _cache.SetStringAsync($"{TokenIdKeyPrefix}{userId}", tokenId,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) });
            
            // Se abbiamo un contesto, salviamo l'associazione nella mappa
            if (!string.IsNullOrEmpty(context))
            {
                await _mapLock.WaitAsync();
                try
                {
                    _contextTokenMap[context] = tokenId;
                    _logger.LogDebug("Associato Token ID {TokenId} al contesto {Context}", tokenId, context);
                }
                finally
                {
                    _mapLock.Release();
                }
            }
        }
        
        return tokenId;
    }
    
    /// <summary>
    /// Salva un token JWT e il relativo refresh token per un utente specifico
    /// </summary>
    public async Task SaveTokensAsync(Guid userId, string jwtToken, string refreshToken, string? context = null)
    {
        var tokenId = await GetOrCreateTokenIdAsync(userId, context);
        
        // Salviamo il token JWT
        await _cache.SetStringAsync($"{JwtTokenKeyPrefix}{tokenId}", jwtToken,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(55) });
        
        // Salviamo il refresh token
        await _cache.SetStringAsync($"{RefreshTokenKeyPrefix}{tokenId}", refreshToken,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) });
        
        _logger.LogInformation("Token salvati per l'utente {UserId}", userId);
    }
    
    /// <summary>
    /// Recupera il token JWT per un utente specifico
    /// </summary>
    public async Task<string?> GetJwtTokenAsync(Guid userId, string? context = null)
    {
        var tokenId = await GetOrCreateTokenIdAsync(userId, context);
        var token = await _cache.GetStringAsync($"{JwtTokenKeyPrefix}{tokenId}");
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token JWT non trovato per l'utente {UserId}", userId);
        }
        
        return token;
    }
    
    /// <summary>
    /// Recupera il refresh token per un utente specifico
    /// </summary>
    public async Task<string?> GetRefreshTokenAsync(Guid userId, string? context = null)
    {
        var tokenId = await GetOrCreateTokenIdAsync(userId, context);
        var token = await _cache.GetStringAsync($"{RefreshTokenKeyPrefix}{tokenId}");
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Refresh token non trovato per l'utente {UserId}", userId);
        }
        
        return token;
    }
    
    /// <summary>
    /// Rimuove tutti i token per un utente specifico
    /// </summary>
    public async Task RemoveTokensAsync(Guid userId, string? context = null)
    {
        var tokenId = await GetOrCreateTokenIdAsync(userId, context);
        
        // Rimuoviamo i token dalla cache
        await _cache.RemoveAsync($"{JwtTokenKeyPrefix}{tokenId}");
        await _cache.RemoveAsync($"{RefreshTokenKeyPrefix}{tokenId}");
        await _cache.RemoveAsync($"{TokenIdKeyPrefix}{userId}");
        
        // Se abbiamo un contesto, rimuoviamo l'associazione dalla mappa
        if (!string.IsNullOrEmpty(context))
        {
            await _mapLock.WaitAsync();
            try
            {
                _contextTokenMap.Remove(context);
                _logger.LogDebug("Rimossa associazione del contesto {Context}", context);
            }
            finally
            {
                _mapLock.Release();
            }
        }
        
        _logger.LogInformation("Token rimossi per l'utente {UserId}", userId);
    }
    
    /// <summary>
    /// Aggiorna un token JWT esistente per un utente specifico
    /// </summary>
    public async Task UpdateJwtTokenAsync(Guid userId, string jwtToken, string? context = null)
    {
        var tokenId = await GetOrCreateTokenIdAsync(userId, context);
        
        // Aggiorniamo il token JWT
        await _cache.SetStringAsync($"{JwtTokenKeyPrefix}{tokenId}", jwtToken,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(55) });
        
        _logger.LogInformation("Token JWT aggiornato per l'utente {UserId}", userId);
    }
    
    /// <summary>
    /// Aggiorna un refresh token esistente per un utente specifico
    /// </summary>
    public async Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, string? context = null)
    {
        var tokenId = await GetOrCreateTokenIdAsync(userId, context);
        
        // Aggiorniamo il refresh token
        await _cache.SetStringAsync($"{RefreshTokenKeyPrefix}{tokenId}", refreshToken,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) });
        
        _logger.LogInformation("Refresh token aggiornato per l'utente {UserId}", userId);
    }
    
    /// <summary>
    /// Registra una associazione tra un contesto e un utente
    /// </summary>
    public async Task RegisterContextAsync(string context, Guid userId)
    {
        var tokenId = await GetOrCreateTokenIdAsync(userId);
        
        await _mapLock.WaitAsync();
        try
        {
            _contextTokenMap[context] = tokenId;
            _logger.LogDebug("Registrato contesto {Context} per l'utente {UserId}", context, userId);
        }
        finally
        {
            _mapLock.Release();
        }
    }
    
    /// <summary>
    /// Rimuove una associazione tra un contesto e un utente
    /// </summary>
    public async Task UnregisterContextAsync(string context)
    {
        await _mapLock.WaitAsync();
        try
        {
            if (_contextTokenMap.Remove(context))
            {
                _logger.LogDebug("Rimosso contesto {Context}", context);
            }
        }
        finally
        {
            _mapLock.Release();
        }
    }
}