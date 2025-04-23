// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\AuthService.cs

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Frontend.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio di autenticazione che utilizza il TokenManager
/// per la gestione dei token
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;
    private readonly IFusionCache _fusionCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenManager _tokenManager;
    
    private const string BaseEndpoint = "api/auth";
    private const string TokenRefreshCacheKey = "TokenRefresh";
    
    // Soglia di tolleranza per la scadenza del token in secondi
    // Se il token scade entro questa soglia, verrà aggiornato preventivamente
    private const int TokenExpiryThresholdSeconds = 30;

    public AuthService(
        HttpClient httpClient,
        IFusionCache fusionCache,
        IHttpContextAccessor httpContextAccessor,
        TokenManager tokenManager,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _fusionCache = fusionCache;
        _httpContextAccessor = httpContextAccessor;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene gli utenti in base ai filtri specificati
    /// </summary>
    public async Task<Option<IEnumerable<UserDto>>> GetUsersAsync(UserFilter filter)
    {
        // Utilizzo diretto del metodo helper con query string
        return await _httpClient.Get<IEnumerable<UserDto>>(BaseEndpoint, filter);
    }

    /// <summary>
    /// Effettua il login di un utente
    /// </summary>
    public async Task<Option<AuthResponse>> LoginAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Tentativo di login con username o password vuoti");
            return Option<AuthResponse>.ValidationError("Username e password sono obbligatori");
        }

        _logger.LogInformation("Tentativo di login per utente: {Username}", username);

        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var result = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/login", request);
        
        if (result.IsSuccess)
        {
            // Utilizza il TokenManager per salvare i token
            await _tokenManager.SetTokensAsync(result.Value.Token, result.Value.RefreshToken, result.Value.UserId);
            
            // Invalida cache
            await _fusionCache.RemoveAsync($"User-Claims-{result.Value.UserId}");
            _logger.LogDebug("Token salvati e cache invalidata");
        }
        
        return result;
    }

    /// <summary>
    /// Registra un nuovo utente
    /// </summary>
    public async Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            _logger.LogWarning("Tentativo di registrazione con dati incompleti");
            return Option<AuthResponse>.ValidationError("I dati di registrazione sono incompleti");
        }

        _logger.LogInformation("Tentativo di registrazione per utente: {Username}", request.Username);

        var result = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/register", request);
        
        if (result.IsSuccess)
        {
            // Salva i token dopo la registrazione
            await _tokenManager.SetTokensAsync(result.Value.Token, result.Value.RefreshToken, result.Value.UserId);
            _logger.LogDebug("Token salvati dopo la registrazione");
        }
        
        return result;
    }

    public async Task<Option<string>> GetSecurityStampAsync()
    {
        return await _httpClient.Get<string>($"{BaseEndpoint}/security-stamp");
    }

    public async Task<Option<ClaimsPrincipal>> GetUserClaims()
    {
        // Ottieni l'ID utente corrente dal cookie di autenticazione
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Impossibile ottenere l'ID utente corrente");
            return Option<ClaimsPrincipal>.ServerError("Utente non autenticato");
        }
        
        // Prova a ottenere l'access token
        var accessTokenOption = await _tokenManager.GetAccessTokenAsync(userId);
        
        if (accessTokenOption.IsSuccess)
        {
            return _tokenManager.GetClaimsFromToken(accessTokenOption.Value);
        }

        // Prova a ottenere il refresh token
        var refreshTokenOption = await _tokenManager.GetRefreshTokenAsync(userId);
        if (!refreshTokenOption.IsSuccess)
        {
            _logger.LogWarning("Nessun refresh token disponibile per l'utente {UserId}", userId);
            return Option<ClaimsPrincipal>.ServerError("Nessun token di autenticazione");
        }

        // Tenta il refresh
        var response = await RefreshTokenAsync(refreshTokenOption.Value);
        if (response.IsSuccess)
        {
            // Verifica che il TokenManager abbia salvato il nuovo token
            var newTokenOption = await _tokenManager.GetAccessTokenAsync(userId);
            if (newTokenOption.IsSuccess)
            {
                return _tokenManager.GetClaimsFromToken(newTokenOption.Value);
            }
        }

        // Se il refresh token non è valido, l'utente deve effettuare il login
        _logger.LogWarning("Refresh token non valido, l'utente deve effettuare il login");
        return Option<ClaimsPrincipal>.ServerError("Refresh token non valido");
    }

    /// <summary>
    /// Aggiorna il token di accesso utilizzando un refresh token
    /// </summary>
    public async Task<Option<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Tentativo di refresh token con token vuoto");
            return Option<AuthResponse>.ValidationError("Il refresh token è obbligatorio");
        }

        _logger.LogInformation("Tentativo di refresh token");

        // Crea la richiesta di refresh token
        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Chiamata diretta all'endpoint di refresh
        var response = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/refresh", request);
        
        if (response.IsSuccess)
        {
            // Salva i nuovi token ottenuti
            await _tokenManager.SetTokensAsync(response.Value.Token, response.Value.RefreshToken, response.Value.UserId);
            _logger.LogInformation("Refresh token completato con successo");
            
            // Invalida eventuali cache correlate
            if (response.Value.UserId != Guid.Empty)
            {
                await _fusionCache.RemoveAsync($"User-Claims-{response.Value.UserId}");
            }
        }
        else
        {
            _logger.LogWarning("Refresh token fallito: {Error}", response.Message);
        }
        
        return response;
    }
    
    /// <summary>
    /// Ottiene un token JWT valido, effettuando il refresh se necessario
    /// </summary>
    public async Task<Option<string>> GetValidAccessTokenAsync()
    {
        // Ottieni l'ID utente corrente
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Impossibile ottenere l'ID utente corrente per il token");
            return Option<string>.ServerError("Utente non autenticato");
        }
        
        // Verifica se esiste un token che sia ancora valido per un periodo ragionevole
        var accessTokenOption = await _tokenManager.GetAccessTokenAsync(userId);
        
        if (accessTokenOption.IsSuccess)
        {
            // Controlla anche la validità del token - se sta per scadere, fai refresh preventivo
            var remainingLifetime = await _tokenManager.GetTokenRemainingLifetimeSecondsAsync(userId);
            if (remainingLifetime > TokenExpiryThresholdSeconds)
            {
                // Il token è ancora valido per un tempo sufficiente
                return accessTokenOption;
            }
            
            _logger.LogInformation("Token sta per scadere (entro {Seconds} secondi), refresh preventivo", 
                TokenExpiryThresholdSeconds);
        }

        // Se non abbiamo un token valido o sta per scadere, tentiamo di fare refresh
        // Utilizziamo FusionCache per evitare refresh multipli simultanei (protezione anti-stampede)
        try
        {
            var cacheKey = $"{TokenRefreshCacheKey}_{userId}";
            var refreshResult = await _fusionCache.GetOrSetAsync(
                cacheKey,
                async _ => {
                    var refreshTokenOption = await _tokenManager.GetRefreshTokenAsync(userId);
                    if (!refreshTokenOption.IsSuccess)
                    {
                        return false;
                    }
                    
                    var authResponse = await RefreshTokenAsync(refreshTokenOption.Value);
                    return authResponse.IsSuccess;
                },
                options => options
                    .SetDuration(TimeSpan.FromSeconds(5))  // Breve durata per evitare problemi con token non validi
                    .SetFailSafe(false),  // Non vogliamo usare valori scaduti
                default);

            if (refreshResult)
            {
                var newTokenOption = await _tokenManager.GetAccessTokenAsync(userId);
                if (newTokenOption.IsSuccess)
                {
                    return newTokenOption;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il refresh del token per l'utente {UserId}", userId);
        }
        
        // Se il token non è più valido e il refresh è fallito
        if (accessTokenOption.IsSuccess)
        {
            // Ritorna comunque il vecchio token (potrebbe ancora funzionare in base alle politiche del server)
            return accessTokenOption;
        }
        
        // Se non c'è proprio un token
        return Option<string>.ServerError("Nessun token di autenticazione disponibile");
    }

    /// <summary>
    /// Esegue il logout dell'utente
    /// </summary>
    public async Task LogoutAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            // Cancella i token
            await _tokenManager.ClearTokensAsync(userId);
            
            // Invalida cache correlate
            await _fusionCache.RemoveAsync($"User-Claims-{userId}");
            await _fusionCache.RemoveAsync($"{TokenRefreshCacheKey}_{userId}");
            
            _logger.LogInformation("Logout completato per l'utente {UserId}", userId);
        }
        
        // Cancella il cookie di autenticazione
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    private Option<ClaimsPrincipal> GetClaimsPrincipal(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var claims = token.Claims.ToList();
        // Aggiungi il claim di autenticazione
        claims.Add(new Claim(ClaimTypes.NameIdentifier, token.Subject));
        return Option<ClaimsPrincipal>.Success(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }
    
    /// <summary>
    /// Ottiene l'ID utente corrente dal cookie di autenticazione
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
        }
        
        return Guid.Empty;
    }
}

