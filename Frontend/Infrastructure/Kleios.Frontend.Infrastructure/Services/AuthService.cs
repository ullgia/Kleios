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
/// Implementazione del servizio di autenticazione che utilizza il TokenDistributionService
/// per la gestione dei token
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;
    private readonly IFusionCache _fusionCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenDistributionService _tokenDistributionService;
    
    private const string BaseEndpoint = "api/auth";
    private const string TokenRefreshCacheKey = "TokenRefresh";

    public AuthService(
        HttpClient httpClient,
        IFusionCache fusionCache,
        IHttpContextAccessor httpContextAccessor,
        ITokenDistributionService tokenDistributionService,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _fusionCache = fusionCache;
        _httpContextAccessor = httpContextAccessor;
        _tokenDistributionService = tokenDistributionService;
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

        var result = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/login", request);
        
        if (result.IsSuccess)
        {
            // Prendiamo l'ID di correlazione dalla richiesta HTTP se disponibile
            string? correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;
            
            // Utilizza il TokenDistributionService per salvare i token
            await _tokenDistributionService.SaveTokensAsync(
                result.Value.UserId, 
                result.Value.Token, 
                result.Value.RefreshToken, 
                correlationId);
            
            // Invalida cache
            await _fusionCache.RemoveAsync($"User-Claims-{result.Value.UserId}");
            _logger.LogDebug("Token salvati e cache invalidata");
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
        
        // Prendiamo l'ID di correlazione dalla richiesta HTTP se disponibile
        string? correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        
        // Utilizza il TokenDistributionService per ottenere un token valido
        var tokenResult = await _tokenDistributionService.GetValidTokenAsync(userId, correlationId);
        
        if (tokenResult.IsSuccess)
        {
            return GetClaimsPrincipal(tokenResult.Value);
        }
        
        // Se il token non è valido, l'utente deve effettuare il login
        _logger.LogWarning("Token non valido, l'utente deve effettuare il login");
        return Option<ClaimsPrincipal>.ServerError("Token non valido");
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
            // Prendiamo l'ID di correlazione dalla richiesta HTTP se disponibile
            string? correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;
            
            // Utilizza il TokenDistributionService per salvare i nuovi token
            await _tokenDistributionService.SaveTokensAsync(
                response.Value.UserId,
                response.Value.Token,
                response.Value.RefreshToken,
                correlationId);
            
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
        
        // Prendiamo l'ID di correlazione dalla richiesta HTTP se disponibile
        string? correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        
        // Utilizza il TokenDistributionService per ottenere un token valido
        var tokenResult = await _tokenDistributionService.GetValidTokenAsync(userId, correlationId);
        
        if (!tokenResult.IsSuccess)
        {
            _logger.LogWarning("Impossibile ottenere un token valido: {Error}", tokenResult.Message);
        }
        
        return tokenResult;
    }

    /// <summary>
    /// Esegue il logout dell'utente
    /// </summary>
    public async Task LogoutAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            // Prendiamo l'ID di correlazione dalla richiesta HTTP se disponibile
            string? correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;
            
            // Utilizza il TokenDistributionService per invalidare i token
            await _tokenDistributionService.InvalidateTokensAsync(userId, correlationId);
            
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

