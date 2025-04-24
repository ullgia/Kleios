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
    
    private const string BaseEndpoint = "api/auth";
    private const string TokenRefreshCacheKey = "TokenRefresh";
    
    // Soglia di tolleranza per la scadenza del token in secondi
    // Se il token scade entro questa soglia, verrà aggiornato preventivamente
    private const int TokenExpiryThresholdSeconds = 30;

    public AuthService(
        HttpClient httpClient,
        IFusionCache fusionCache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _fusionCache = fusionCache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
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

        return await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/login", request);
        
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

        var token = await GetValidAccessTokenAsync();
        if (!token.IsSuccess)
        {
            _logger.LogWarning("Impossibile ottenere un token valido: {Error}", token.Message);
            return Option<ClaimsPrincipal>.ServerError("Impossibile ottenere un token valido");
        }
        // Decodifica il token JWT per ottenere i claims
        var claimsPrincipal = GetClaimsPrincipal(token.Value);
        if (!claimsPrincipal.IsSuccess)
        {
            _logger.LogWarning("Impossibile decodificare il token JWT: {Error}", claimsPrincipal.Message);
            return Option<ClaimsPrincipal>.ServerError("Impossibile decodificare il token JWT");
        }
        return claimsPrincipal;
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

        return await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/refresh", request);
    }
    
    /// <summary>
    /// Ottiene un token JWT valido, effettuando il refresh se necessario
    /// </summary>
    public async Task<Option<string>> GetValidAccessTokenAsync()
    {
        // get the token from the cookies 
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext non disponibile");
            return Option<string>.ServerError("HttpContext non disponibile");
        }
        var token = httpContext.Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token di refresh non trovato nei cookie");
            return Option<string>.ServerError("Token di refresh non trovato");
        }

        var accessToken = await RefreshTokenAsync(token);
        if (!accessToken.IsSuccess)
        {
            _logger.LogWarning("Impossibile ottenere il token di accesso: {Error}", accessToken.Message);
            return Option<string>.ServerError("Impossibile ottenere il token di accesso");
        }

        return accessToken.Value.Token;
    }

    /// <summary>
    /// Esegue il logout dell'utente
    /// </summary>
    public async Task LogoutAsync()
    {
        throw new NotImplementedException();
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

