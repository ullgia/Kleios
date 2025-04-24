// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\TokenDistributionService.cs
using Microsoft.AspNetCore.Components.Server.Circuits;
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Servizio che gestisce la distribuzione e il recupero dei token JWT
/// indipendentemente dal modello di rendering (server o client)
/// </summary>
public class TokenDistributionService : ITokenDistributionService, ICircuitHandler
{
    private readonly ITokenStore _tokenStore;
    private readonly ITokenRefreshService _tokenRefreshService;
    private readonly ILogger<TokenDistributionService> _logger;
    
    // Soglia di tolleranza per la scadenza del token in secondi
    private const int TokenExpiryThresholdSeconds = 30;
    
    public TokenDistributionService(
        ITokenStore tokenStore,
        ITokenRefreshService tokenRefreshService,
        ILogger<TokenDistributionService> logger)
    {
        _tokenStore = tokenStore;
        _tokenRefreshService = tokenRefreshService;
        _logger = logger;
    }
    
    /// <summary>
    /// Ottiene un token JWT valido per l'utente, usando il refresh token se necessario
    /// </summary>
    public async Task<Option<string>> GetValidTokenAsync(Guid userId, string? circuitId = null)
    {
        try
        {
            // Proviamo a recuperare il token JWT
            var token = await _tokenStore.GetJwtTokenAsync(userId, circuitId);
            
            // Se il token non esiste o è scaduto, proviamo a rigenerarlo con il refresh token
            if (string.IsNullOrEmpty(token) || IsTokenExpiredOrExpiringSoon(token, TokenExpiryThresholdSeconds))
            {
                _logger.LogInformation("Token JWT non valido o in scadenza, tentativo di refresh");
                
                // Otteniamo il refresh token
                var refreshToken = await _tokenStore.GetRefreshTokenAsync(userId, circuitId);
                
                // Se abbiamo un refresh token, lo usiamo per ottenere un nuovo token
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var response = await _tokenRefreshService.RefreshTokenAsync(refreshToken);
                    if (response.IsSuccess)
                    {
                        token = response.Value.Token;
                        
                        // Aggiorniamo i token
                        await _tokenStore.UpdateJwtTokenAsync(userId, token, circuitId);
                        await _tokenStore.UpdateRefreshTokenAsync(userId, response.Value.RefreshToken, circuitId);
                        
                        _logger.LogInformation("Refresh token completato con successo");
                    }
                    else
                    {
                        _logger.LogWarning("Refresh token fallito: {Error}", response.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("Refresh token non disponibile per l'utente {UserId}", userId);
                }
            }
            
            return string.IsNullOrEmpty(token) 
                ? Option<string>.Unauthorized("Token non disponibile") 
                : Option<string>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero o refresh del token per l'utente {UserId}", userId);
            return Option<string>.ServerError("Errore nella gestione del token di autenticazione");
        }
    }
    
    /// <summary>
    /// Controlla se un token JWT è scaduto o sta per scadere
    /// </summary>
    private bool IsTokenExpiredOrExpiringSoon(string token, int thresholdSeconds)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var now = DateTime.UtcNow;
            var expiry = jwtToken.ValidTo;
            
            // Controlliamo se il token è già scaduto o scadrà entro thresholdSeconds
            return now >= expiry || (expiry - now).TotalSeconds <= thresholdSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la verifica della scadenza del token");
            return true; // In caso di errore, consideriamo il token come scaduto
        }
    }
    
    /// <summary>
    /// Salva i token dopo il login o la registrazione
    /// </summary>
    public async Task SaveTokensAsync(Guid userId, string token, string refreshToken, string? circuitId = null)
    {
        try 
        {
            await _tokenStore.SaveTokensAsync(userId, token, refreshToken, circuitId);
            _logger.LogInformation("Token salvati per l'utente {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio dei token per l'utente {UserId}", userId);
            throw; // Propaghiamo l'eccezione per gestirla ai livelli superiori
        }
    }
    
    /// <summary>
    /// Invalida i token dell'utente (logout)
    /// </summary>
    public async Task InvalidateTokensAsync(Guid userId, string? circuitId = null)
    {
        try 
        {
            await _tokenStore.RemoveTokensAsync(userId, circuitId);
            _logger.LogInformation("Token invalidati per l'utente {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invalidazione dei token per l'utente {UserId}", userId);
            throw; // Propaghiamo l'eccezione per gestirla ai livelli superiori
        }
    }
    
    #region ICircuitHandler
    
    public Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Nessuna operazione necessaria all'apertura del circuito
        return Task.CompletedTask;
    }
    
    public Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Quando il circuito viene chiuso, rimuoviamo la sua associazione con il tokenId
        if (_tokenStore is DistributedCacheTokenStore cacheStore)
        {
            return cacheStore.UnregisterContextAsync(circuit.Id);
        }
        return Task.CompletedTask;
    }
    
    public Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Possiamo implementare logica specifica quando la connessione cade
        return Task.CompletedTask;
    }
    
    public Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Possiamo implementare logica specifica quando la connessione ritorna attiva
        return Task.CompletedTask;
    }
    
    #endregion
}