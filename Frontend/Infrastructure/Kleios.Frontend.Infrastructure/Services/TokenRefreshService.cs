// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\TokenRefreshService.cs
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Frontend.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio di refresh dei token con sicurezza migliorata.
/// Questo servizio è responsabile esclusivamente di effettuare il refresh dei token
/// senza dipendere da altri servizi di autenticazione, risolvendo così la dipendenza circolare.
/// </summary>
public class TokenRefreshService : ITokenRefreshService
{
    private readonly HttpClient _httpClient;
    private readonly TokenSecurityService _tokenSecurityService;
    private readonly ILogger<TokenRefreshService> _logger;
    
    private const string BaseEndpoint = "api/auth";
    
    // Delay progressivo per tentativi falliti (mitigazione attacchi brute force)
    private static readonly Dictionary<string, (int attempts, DateTime lastAttempt)> _failedAttempts = new();
    private static readonly SemaphoreSlim _failedAttemptsLock = new(1, 1);
    private const int MaxAttemptsBeforeCooldown = 5;
    
    public TokenRefreshService(
        HttpClient httpClient, 
        TokenSecurityService tokenSecurityService,
        ILogger<TokenRefreshService> logger)
    {
        _httpClient = httpClient;
        _tokenSecurityService = tokenSecurityService;
        _logger = logger;
    }
    
    /// <summary>
    /// Effettua il refresh di un token JWT usando un refresh token
    /// </summary>
    public async Task<Option<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Tentativo di refresh token con token vuoto");
            return Option<AuthResponse>.ValidationError("Il refresh token è obbligatorio");
        }

        // Calcoliamo un identificatore univoco per questo refresh token (hash parziale)
        // Usiamo solo i primi 8 caratteri per non esporre informazioni sensibili nei log
        var tokenIdentifier = refreshToken.Length > 8 
            ? refreshToken.Substring(0, 8) 
            : refreshToken;
            
        // Verifichiamo se il client è in cooldown per troppi tentativi falliti
        if (!await CheckRateLimitAsync(tokenIdentifier))
        {
            _logger.LogWarning("Limite di rate superato per tentativi di refresh token");
            return Option<AuthResponse>.TooManyRequests("Troppi tentativi di refresh, riprovare più tardi");
        }

        _logger.LogInformation("Tentativo di refresh token");

        try 
        {
            // Verifichiamo se il token è sicuro (non riutilizzato)
            // Poiché non abbiamo ancora l'ID utente (lo otterremo dopo il refresh),
            // passiamo un ID vuoto che verrà ignorato per questa verifica
            if (!await _tokenSecurityService.CanUseRefreshTokenAsync(refreshToken, Guid.Empty))
            {
                _logger.LogWarning("Token considerato non sicuro, refresh non autorizzato");
                await IncrementFailedAttemptsAsync(tokenIdentifier);
                return Option<AuthResponse>.Unauthorized("Token non valido o compromesso");
            }
            
            // Crea la richiesta di refresh token
            var request = new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            };

            // Chiamata all'endpoint di refresh
            var response = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/refresh", request);
            
            if (response.IsSuccess)
            {
                _logger.LogInformation("Refresh token completato con successo");
                await ResetFailedAttemptsAsync(tokenIdentifier);
            }
            else
            {
                _logger.LogWarning("Refresh token fallito: {Error}", response.Message);
                await IncrementFailedAttemptsAsync(tokenIdentifier);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il refresh del token");
            await IncrementFailedAttemptsAsync(tokenIdentifier);
            return Option<AuthResponse>.ServerError("Si è verificato un errore durante il refresh del token");
        }
    }
    
    /// <summary>
    /// Verifica se il client ha superato il rate limit
    /// </summary>
    private async Task<bool> CheckRateLimitAsync(string tokenIdentifier)
    {
        await _failedAttemptsLock.WaitAsync();
        try
        {
            if (_failedAttempts.TryGetValue(tokenIdentifier, out var attempts))
            {
                if (attempts.attempts >= MaxAttemptsBeforeCooldown)
                {
                    // Calcoliamo il tempo di cooldown in base al numero di tentativi falliti
                    // Più tentativi falliti, più lungo sarà il cooldown
                    var cooldownSeconds = Math.Min(30, Math.Pow(2, attempts.attempts - MaxAttemptsBeforeCooldown));
                    var cooldownPeriod = TimeSpan.FromSeconds(cooldownSeconds);
                    
                    if (DateTime.UtcNow - attempts.lastAttempt < cooldownPeriod)
                    {
                        _logger.LogWarning("Client in cooldown per {Seconds} secondi", cooldownSeconds);
                        return false;
                    }
                }
            }
            
            return true;
        }
        finally
        {
            _failedAttemptsLock.Release();
        }
    }
    
    /// <summary>
    /// Incrementa il contatore dei tentativi falliti
    /// </summary>
    private async Task IncrementFailedAttemptsAsync(string tokenIdentifier)
    {
        await _failedAttemptsLock.WaitAsync();
        try
        {
            if (_failedAttempts.TryGetValue(tokenIdentifier, out var current))
            {
                _failedAttempts[tokenIdentifier] = (current.attempts + 1, DateTime.UtcNow);
            }
            else
            {
                _failedAttempts[tokenIdentifier] = (1, DateTime.UtcNow);
            }
        }
        finally
        {
            _failedAttemptsLock.Release();
        }
    }
    
    /// <summary>
    /// Resetta il contatore dei tentativi falliti
    /// </summary>
    private async Task ResetFailedAttemptsAsync(string tokenIdentifier)
    {
        await _failedAttemptsLock.WaitAsync();
        try
        {
            _failedAttempts.Remove(tokenIdentifier);
        }
        finally
        {
            _failedAttemptsLock.Release();
        }
    }
}