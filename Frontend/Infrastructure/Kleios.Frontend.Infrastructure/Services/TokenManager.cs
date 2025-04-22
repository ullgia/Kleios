using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Kleios.Shared;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Classe ad-hoc per la gestione dei token di autenticazione.
/// Mantiene l'access token in memoria e il refresh token nei cookie.
/// </summary>
public class TokenManager
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TokenManager> _logger;
    
    private const string RefreshTokenCookieName = "refresh_token";
    
    // Singola istanza statica del token per l'intera applicazione
    private static string _accessToken;
    private static readonly object _accessTokenLock = new object();
    private static DateTime _accessTokenExpiry = DateTime.MinValue;
    
    public TokenManager(
        IHttpContextAccessor httpContextAccessor,
        ILogger<TokenManager> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    /// <summary>
    /// Salva l'access token in memoria e il refresh token nei cookie
    /// </summary>
    public void SetTokens(string accessToken, string refreshToken)
    {
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Tentativo di salvare token nulli o vuoti");
            return;
        }
        
        SaveAccessTokenInMemory(accessToken);
        SaveRefreshTokenInCookie(refreshToken);
    }
    
    /// <summary>
    /// Tenta di recuperare l'access token dalla memoria
    /// </summary>
    public bool TryGetAccessToken(out string accessToken)
    {
        lock (_accessTokenLock)
        {
            // Verifica se abbiamo un token e se è ancora valido
            if (!string.IsNullOrEmpty(_accessToken) && _accessTokenExpiry > DateTime.UtcNow)
            {
                accessToken = _accessToken;
                return true;
            }
            
            accessToken = null;
            return false;
        }
    }
    
    /// <summary>
    /// Recupera il refresh token dai cookie
    /// </summary>
    public string GetRefreshToken()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken))
        {
            return refreshToken;
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Cancella tutti i token dall'applicazione
    /// </summary>
    public void ClearTokens()
    {
        // Pulisci l'access token in memoria
        lock (_accessTokenLock)
        {
            _accessToken = null;
            _accessTokenExpiry = DateTime.MinValue;
        }
        
        // Rimuovi il cookie del refresh token
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Delete(RefreshTokenCookieName);
            _logger.LogDebug("Refresh token rimosso dal cookie");
        }
        else
        {
            _logger.LogWarning("HttpContext non disponibile per rimuovere il cookie");
        }
    }
    
    /// <summary>
    /// Verifica se il token è ancora valido
    /// </summary>
    public bool IsTokenValid()
    {
        return TryGetAccessToken(out _);
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
    public double GetTokenRemainingLifetimeSeconds()
    {
        lock (_accessTokenLock)
        {
            if (string.IsNullOrEmpty(_accessToken) || _accessTokenExpiry <= DateTime.UtcNow)
            {
                return 0;
            }
            
            var remainingTime = _accessTokenExpiry - DateTime.UtcNow;
            return remainingTime.TotalSeconds;
        }
    }
    
    #region Metodi privati
    
    /// <summary>
    /// Salva l'access token in memoria con la sua scadenza
    /// </summary>
    private void SaveAccessTokenInMemory(string accessToken)
    {
        lock (_accessTokenLock)
        {
            _accessToken = accessToken;
            
            // Calcola la scadenza del token JWT
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);
                _accessTokenExpiry = jwtToken.ValidTo;
                _logger.LogDebug("Access token salvato in memoria con scadenza: {Expiry}", _accessTokenExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il parsing della scadenza del token");
                _accessTokenExpiry = DateTime.UtcNow.AddMinutes(30); // Valore predefinito
                _logger.LogDebug("Usata scadenza predefinita: {Expiry}", _accessTokenExpiry);
            }
        }
    }
    
    /// <summary>
    /// Salva il refresh token in un cookie sicuro
    /// </summary>
    private void SaveRefreshTokenInCookie(string refreshToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30)
            };
            
            httpContext.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
            _logger.LogDebug("Refresh token salvato nel cookie con scadenza: {Expires}", cookieOptions.Expires);
        }
        else
        {
            _logger.LogWarning("HttpContext non disponibile per salvare il cookie");
        }
    }
    
    #endregion
}