using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kleios.Frontend.Shared.Models;
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Classe per la gestione dei token di autenticazione.
/// Utilizza ITokenStorage per memorizzare e recuperare i token.
/// </summary>
public class TokenManager
{
    private readonly ITokenStorage _tokenStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TokenManager> _logger;
    
    public TokenManager(
        ITokenStorage tokenStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TokenManager> logger)
    {
        _tokenStorage = tokenStorage;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    /// <summary>
    /// Salva l'access token e il refresh token
    /// </summary>
    public async Task SetTokensAsync(string accessToken, string refreshToken, Guid userId)
    {
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Tentativo di salvare token nulli o vuoti");
            return;
        }
        
        try
        {
            // Calcola la scadenza del token JWT
            DateTime accessTokenExpiry = DateTime.UtcNow.AddMinutes(30); // Valore di default
            
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);
                accessTokenExpiry = jwtToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il parsing della scadenza del token");
            }
            
            // Salva l'access token
            await _tokenStorage.SaveAccessTokenAsync(userId.ToString(), accessToken, accessTokenExpiry);
            
            // Salva il refresh token (scadenza standard a 7 giorni)
            await _tokenStorage.SaveRefreshTokenAsync(userId.ToString(), refreshToken, DateTime.UtcNow.AddDays(7));
            
            _logger.LogDebug("Token salvati con successo per l'utente {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio dei token per l'utente {UserId}", userId);
        }
    }
    
    /// <summary>
    /// Recupera l'access token di un utente
    /// </summary>
    public async Task<Option<string>> GetAccessTokenAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Tentativo di recuperare un access token con ID utente non valido");
            return Option<string>.ValidationError("ID utente non valido");
        }
        
        try
        {
            var tokenOption = await _tokenStorage.GetAccessTokenAsync(userId.ToString());
            
            if (tokenOption.IsSuccess && tokenOption.Value.Expiry > DateTime.UtcNow)
            {
                return Option<string>.Success(tokenOption.Value.Token);
            }
            
            return Option<string>.NotFound("Nessun token valido trovato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dell'access token per l'utente {UserId}", userId);
            return Option<string>.ServerError($"Errore nel recupero del token: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Recupera il refresh token di un utente
    /// </summary>
    public async Task<Option<string>> GetRefreshTokenAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Tentativo di recuperare un refresh token con ID utente non valido");
            return Option<string>.ValidationError("ID utente non valido");
        }
        
        try
        {
            var tokenOption = await _tokenStorage.GetRefreshTokenAsync(userId.ToString());
            
            if (tokenOption.IsSuccess && tokenOption.Value.Expiry > DateTime.UtcNow)
            {
                return Option<string>.Success(tokenOption.Value.Token);
            }
            
            return Option<string>.NotFound("Nessun token valido trovato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del refresh token per l'utente {UserId}", userId);
            return Option<string>.ServerError($"Errore nel recupero del token: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Cancella tutti i token associati all'utente
    /// </summary>
    public async Task ClearTokensAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Tentativo di cancellare token con ID utente non valido");
            return;
        }
        
        try
        {
            await _tokenStorage.ClearTokensAsync(userId.ToString());
            _logger.LogDebug("Token cancellati con successo per l'utente {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la cancellazione dei token per l'utente {UserId}", userId);
        }
    }
    
    /// <summary>
    /// Verifica se esiste un token valido per l'utente
    /// </summary>
    public async Task<bool> IsTokenValidAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }
        
        var accessToken = await GetAccessTokenAsync(userId);
        return accessToken.IsSuccess;
    }
    
    /// <summary>
    /// Ottiene i claims dal token JWT
    /// </summary>
    public Option<ClaimsPrincipal> GetClaimsFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Crea identity e principal dai claims del token
            var identity = new ClaimsIdentity(jwtToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            return Option<ClaimsPrincipal>.Success(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'analisi del token JWT");
            return Option<ClaimsPrincipal>.ServerError($"Errore nell'analisi del token: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ottiene il tempo rimanente in secondi prima della scadenza del token
    /// </summary>
    public async Task<double> GetTokenRemainingLifetimeSecondsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Tentativo di verificare la scadenza con ID utente non valido");
            return 0;
        }
        
        try
        {
            var tokenOption = await _tokenStorage.GetAccessTokenAsync(userId.ToString());
            
            if (tokenOption.IsSuccess)
            {
                var remainingTime = tokenOption.Value.Expiry - DateTime.UtcNow;
                return remainingTime.TotalSeconds > 0 ? remainingTime.TotalSeconds : 0;
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il controllo della scadenza del token per l'utente {UserId}", userId);
            return 0;
        }
    }
    
    /// <summary>
    /// Estrae l'ID utente da un token JWT
    /// </summary>
    public Guid ExtractUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Cerca l'ID utente nel subject claim (standard per JWT)
            var subClaim = jwtToken.Subject;
            if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var userId))
            {
                return userId;
            }
            
            // Fallback: cerca tra gli altri claims
            var nameIdClaim = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == JwtRegisteredClaimNames.Sub);
                
            if (nameIdClaim != null && Guid.TryParse(nameIdClaim.Value, out userId))
            {
                return userId;
            }
            
            _logger.LogWarning("Impossibile estrarre l'ID utente dal token");
            return Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'estrazione dell'ID utente dal token");
            return Guid.Empty;
        }
    }
}