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
/// Implementazione del servizio di autenticazione frontend che utilizza il TokenDistributionService
/// per la gestione dei token
/// </summary>
public class AuthService : IFrontendAuthService
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
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogError("HttpContext non disponibile durante il login");
                return Option<AuthResponse>.ServerError("Contesto HTTP non disponibile");
            }
            
            // 1. Estrai claims da JWT
            var claims = ExtractClaimsFromJwt(result.Value.Token);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            // 2. Sign-in con Cookie Authentication (ASP.NET Core standard)
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                    AllowRefresh = true
                });
            
            _logger.LogInformation("Cookie di autenticazione creato per utente {UserId}", result.Value.UserId);
            
            // 2. Salva token in cache distribuita (per API calls dal server)
            // IMPORTANTE: circuitId = null per SSR!
            await _tokenDistributionService.SaveTokensAsync(
                result.Value.UserId,
                result.Value.Token,
                result.Value.RefreshToken,
                circuitId: null);  // ← NULL per SSR!
            
            _logger.LogInformation("Token salvati in cache distribuita");
            
            // 3. Invalida cache claims
            await _fusionCache.RemoveAsync($"User-Claims-{result.Value.UserId}");
            
            _logger.LogInformation("Login completato con successo per utente {UserId}", result.Value.UserId);
        }
        
        return result;
    }

    public async Task<Option<string>> GetSecurityStampAsync()
    {
        return await _httpClient.Get<string>($"{BaseEndpoint}/security-stamp");
    }

    public async Task<Option<ClaimsPrincipal>> GetUserClaims(Guid userId)
    {

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
    /// Effettua il logout dell'utente
    /// </summary>
    public async Task<Option<bool>> LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("HttpContext non disponibile durante il logout");
            return Option<bool>.ServerError("Contesto HTTP non disponibile");
        }
        
        // Ottieni userId prima di fare logout
        var userId = GetCurrentUserId();
        
        // Sign-out con Cookie Authentication
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        _logger.LogInformation("Cookie di autenticazione rimosso");
        
        // Rimuovi token dalla cache
        if (userId != Guid.Empty)
        {
            await _tokenDistributionService.InvalidateTokensAsync(userId, circuitId: null);
            await _fusionCache.RemoveAsync($"User-Claims-{userId}");
            
            _logger.LogInformation("Token invalidati in cache per utente {UserId}", userId);
        }
        
        _logger.LogInformation("Logout completato per utente {UserId}", userId);
        
        return Option<bool>.Success(true);
    }
    
    /// <summary>
    /// Estrae claims da un JWT token
    /// </summary>
    private List<Claim> ExtractClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        var claims = token.Claims.ToList();
        
        // Assicurati che ci sia il NameIdentifier claim
        if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier) && !string.IsNullOrEmpty(token.Subject))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, token.Subject));
        }
        
        return claims;
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

