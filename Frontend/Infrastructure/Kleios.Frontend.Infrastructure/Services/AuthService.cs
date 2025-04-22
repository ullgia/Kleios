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
            _tokenManager.SetTokens(result.Value.Token, result.Value.RefreshToken);
            
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

        return await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/register", request);
    }

    public async Task<Option<string>> GetSecurityStampAsync()
    {
        return await _httpClient.Get<string>($"{BaseEndpoint}/security-stamp");
    }

    public async Task<Option<ClaimsPrincipal>> GetUserClaims()
    {
        if (_tokenManager.TryGetAccessToken(out var accessToken))
        {
            return GetClaimsPrincipal(accessToken);
        }

        var refreshToken = _tokenManager.GetRefreshToken();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Option<ClaimsPrincipal>.ServerError("no access token");
        }

        var response = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/refresh", refreshToken);
        if (response.IsSuccess)
        {
            // Salva i nuovi token
            _tokenManager.SetTokens(response.Value.Token, response.Value.RefreshToken);
            return GetClaimsPrincipal(response.Value.Token);
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
            _tokenManager.SetTokens(response.Value.Token, response.Value.RefreshToken);
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
        // Verifica se esiste un token che sia ancora valido per un periodo ragionevole
        if (_tokenManager.TryGetAccessToken(out var accessToken))
        {
            // Controlla anche la validità del token - se sta per scadere, fai refresh preventivo
            if (_tokenManager.GetTokenRemainingLifetimeSeconds() > TokenExpiryThresholdSeconds)
            {
                // Il token è ancora valido per un tempo sufficiente
                return Option<string>.Success(accessToken);
            }
            
            _logger.LogInformation("Token sta per scadere (entro {Seconds} secondi), refresh preventivo", 
                TokenExpiryThresholdSeconds);
        }

        // Se non abbiamo un token valido o sta per scadere, tentiamo di fare refresh
        // Utilizziamo FusionCache per evitare refresh multipli simultanei (protezione anti-stampede)
        try
        {
            var refreshResult = await _fusionCache.GetOrSetAsync(
                TokenRefreshCacheKey,
                async _ => {
                    var refreshToken = _tokenManager.GetRefreshToken();
                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        return false;
                    }
                    
                    var authResponse = await RefreshTokenAsync(refreshToken);
                    return authResponse.IsSuccess;
                },
                options => options
                    .SetDuration(TimeSpan.FromSeconds(5))  // Breve durata per evitare problemi con token non validi
                    .SetFailSafe(false),  // Non vogliamo usare valori scaduti
                default);

            if (refreshResult && _tokenManager.TryGetAccessToken(out var newToken))
            {
                return Option<string>.Success(newToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il refresh del token");
        }
        
        // Se il token non è più valido e il refresh è fallito
        if (!string.IsNullOrEmpty(accessToken))
        {
            // Ritorna comunque il vecchio token (potrebbe ancora funzionare in base alle politiche del server)
            return Option<string>.Success(accessToken);
        }
        
        // Se non c'è proprio un token
        return Option<string>.ServerError("Nessun token di autenticazione disponibile");
    }

    private Option<ClaimsPrincipal> GetClaimsPrincipal(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var claims = token.Claims.ToList();
        // Aggiungi il claim di autenticazione
        claims.Add(new Claim(ClaimTypes.NameIdentifier, token.Subject));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}

