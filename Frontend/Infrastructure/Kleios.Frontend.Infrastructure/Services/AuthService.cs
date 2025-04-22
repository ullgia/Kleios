// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\AuthService.cs

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Frontend.Infrastructure.Helpers;
using Kleios.Frontend.Infrastructure.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
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

        var response = await _httpClient.Get<AuthResponse>($"{BaseEndpoint}/refresh-token", refreshToken);
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


    private Option<ClaimsPrincipal> GetClaimsPrincipal(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var claims = token.Claims.ToList();
        // Aggiungi il claim di autenticazione
        claims.Add(new Claim(ClaimTypes.Name, token.Subject));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}

