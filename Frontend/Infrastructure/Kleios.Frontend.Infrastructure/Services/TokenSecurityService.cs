// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\TokenSecurityService.cs
using System.Security.Cryptography;
using Kleios.Frontend.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Servizio per la gestione della sicurezza dei token.
/// Implementa meccanismi per prevenire attacchi di riutilizzo dei token e rilevare potenziali compromissioni.
/// </summary>
public class TokenSecurityService
{
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<TokenSecurityService> _logger;
    
    // Utilizziamo una cache temporanea per tenere traccia dei token utilizzati recentemente
    // per prevenire attacchi di riutilizzo
    private static readonly Dictionary<string, DateTime> _usedTokenHashes = new();
    private static readonly SemaphoreSlim _usedTokensLock = new(1, 1);
    
    // Intervallo di pulizia della cache dei token utilizzati (per evitare memory leak)
    private const int CleanupIntervalMinutes = 10;
    private static DateTime _lastCleanupTime = DateTime.UtcNow;
    
    public TokenSecurityService(ITokenStore tokenStore, ILogger<TokenSecurityService> logger)
    {
        _tokenStore = tokenStore;
        _logger = logger;
    }
    
    /// <summary>
    /// Verifica se un refresh token può essere utilizzato in modo sicuro.
    /// Implementa meccanismi di protezione contro il riutilizzo dei token e altre vulnerabilità.
    /// </summary>
    /// <param name="refreshToken">Il refresh token da verificare</param>
    /// <param name="userId">L'ID dell'utente associato al token</param>
    /// <returns>True se il token può essere utilizzato in modo sicuro, false altrimenti</returns>
    public async Task<bool> CanUseRefreshTokenAsync(string refreshToken, Guid userId)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Tentativo di verifica di un refresh token vuoto");
            return false;
        }
        
        try
        {
            // Pulizia periodica della cache dei token utilizzati
            await CleanupTokenCacheIfNeededAsync();
            
            // Calcoliamo l'hash del token per evitare di memorizzare il token in chiaro
            var tokenHash = ComputeHash(refreshToken);
            
            // Verifichiamo se il token è già stato utilizzato recentemente
            if (await HasTokenBeenUsedRecentlyAsync(tokenHash))
            {
                _logger.LogWarning("Tentativo di riutilizzo del refresh token rilevato per l'utente {UserId}", userId);
                
                // Invalidiamo tutti i token dell'utente come misura di sicurezza
                await _tokenStore.RemoveTokensAsync(userId);
                
                return false;
            }
            
            // Registriamo l'utilizzo del token
            await RegisterTokenUseAsync(tokenHash);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la verifica di sicurezza del refresh token per l'utente {UserId}", userId);
            return false;
        }
    }
    
    /// <summary>
    /// Calcola l'hash di un token per la memorizzazione sicura
    /// </summary>
    private string ComputeHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
    
    /// <summary>
    /// Verifica se un token è stato utilizzato recentemente
    /// </summary>
    private async Task<bool> HasTokenBeenUsedRecentlyAsync(string tokenHash)
    {
        await _usedTokensLock.WaitAsync();
        try
        {
            return _usedTokenHashes.ContainsKey(tokenHash);
        }
        finally
        {
            _usedTokensLock.Release();
        }
    }
    
    /// <summary>
    /// Registra l'utilizzo di un token
    /// </summary>
    private async Task RegisterTokenUseAsync(string tokenHash)
    {
        await _usedTokensLock.WaitAsync();
        try
        {
            _usedTokenHashes[tokenHash] = DateTime.UtcNow;
        }
        finally
        {
            _usedTokensLock.Release();
        }
    }
    
    /// <summary>
    /// Rimuove i token vecchi dalla cache per evitare memory leak
    /// </summary>
    private async Task CleanupTokenCacheIfNeededAsync()
    {
        var now = DateTime.UtcNow;
        
        // Eseguiamo la pulizia solo a intervalli regolari
        if ((now - _lastCleanupTime).TotalMinutes < CleanupIntervalMinutes)
        {
            return;
        }
        
        await _usedTokensLock.WaitAsync();
        try
        {
            _lastCleanupTime = now;
            
            // Rimuoviamo i token più vecchi di 24 ore
            var cutoff = now.AddHours(-24);
            var keysToRemove = _usedTokenHashes
                .Where(pair => pair.Value < cutoff)
                .Select(pair => pair.Key)
                .ToList();
                
            foreach (var key in keysToRemove)
            {
                _usedTokenHashes.Remove(key);
            }
            
            _logger.LogInformation("Pulizia cache token completata. Rimossi {Count} token vecchi", keysToRemove.Count);
        }
        finally
        {
            _usedTokensLock.Release();
        }
    }
    
    /// <summary>
    /// Genera un nuovo refresh token sicuro
    /// </summary>
    public static string GenerateSecureRefreshToken()
    {
        var randomBytes = new byte[64]; // 512 bit
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}