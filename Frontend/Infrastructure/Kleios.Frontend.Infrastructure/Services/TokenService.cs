// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\TokenService.cs
using Kleios.Frontend.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Kleios.Shared;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Servizio che gestisce i token di autenticazione con UserInfoState e cookies
/// </summary>
public class TokenService : ITokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;
    private readonly UserInfoState _userInfoState;
    private readonly ILocalStorageService _localStorage;
    private const string RefreshTokenKey = "refresh_token";
    private const int CookieExpirationDays = 7;

    public TokenService(
        ILocalStorageService localStorage,
        IHttpContextAccessor httpContextAccessor,
        NavigationManager navigationManager,
        UserInfoState userInfoState)
    {
        _localStorage = localStorage;
        _httpContextAccessor = httpContextAccessor;
        _navigationManager = navigationManager;
        _userInfoState = userInfoState;
    }

    /// <summary>
    /// Ottiene il token JWT dallo stato dell'utente o, come fallback, dal localStorage
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        // Verifica prima nello stato dell'utente (funziona sempre, anche durante prerendering)
        if (_userInfoState.IsAuthenticated)
        {
            return _userInfoState.AccessToken;
        }
        
        // Come fallback, controlla nel localStorage (solo lato client)
        try
        {
            return await _localStorage.GetAsync<string>("access_token");
        }
        catch
        {
            // Ignora eccezioni durante il prerendering
            return null;
        }
    }

    /// <summary>
    /// Ottiene il refresh token dal cookie
    /// </summary>
    public Task<string?> GetRefreshTokenAsync()
    {
        // Controlla il cookie (funziona sia lato server che client)
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext != null && httpContext.Request.Cookies.TryGetValue(RefreshTokenKey, out var cookieToken))
        {
            return Task.FromResult<string?>(cookieToken);
        }
        
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Salva i token di autenticazione (access token nello stato dell'utente, refresh token nel cookie)
    /// </summary>
    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        // Salva il refresh token nel cookie (accessibile lato server)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _navigationManager.BaseUri.StartsWith("https"),
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(CookieExpirationDays)
            };
            
            httpContext.Response.Cookies.Append(RefreshTokenKey, refreshToken, cookieOptions);
        }
        
        // Aggiorna lo stato dell'utente con le informazioni dal token
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);
            _userInfoState.UpdateFromToken(accessToken, jwtToken.Claims);
        }
        catch
        {
            // In caso di errore nell'analisi del token
        }
        
        // Come fallback, salva anche nel localStorage per casi in cui lo stato scoped non è disponibile
        try
        {
            await _localStorage.SetAsync("access_token", accessToken);
        }
        catch
        {
            // Ignora eccezioni durante il prerendering
        }
    }

    /// <summary>
    /// Rimuove entrambi i token di autenticazione
    /// </summary>
    public async Task RemoveTokensAsync()
    {
        // Rimuovi il refresh token dal cookie
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Delete(RefreshTokenKey);
        }
        
        // Resetta lo stato dell'utente
        _userInfoState.Reset();
        
        // Come fallback, rimuovi anche dal localStorage
        try
        {
            await _localStorage.RemoveAsync("access_token");
        }
        catch
        {
            // Ignora eccezioni durante il prerendering
        }
    }

    /// <summary>
    /// Verifica se esiste un token valido
    /// </summary>
    public async Task<bool> HasValidTokenAsync()
    {
        // Verifica prima lo stato dell'utente
        if (_userInfoState.IsAuthenticated)
        {
            // Verifica se il token nello stato è scaduto
            return !IsTokenExpired(_userInfoState.AccessToken);
        }
        
        // Se non c'è un token nello stato, verifica il refresh token
        var refreshToken = await GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            return true; // Se c'è un refresh token, possiamo usarlo per generare un nuovo access token
        }
        
        // Come fallback, verifica nel localStorage
        try
        {
            var accessToken = await _localStorage.GetAsync<string>("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                return !IsTokenExpired(accessToken);
            }
        }
        catch
        {
            // Ignora eccezioni durante il prerendering
        }
        
        return false;
    }
    
    /// <summary>
    /// Controlla se un token JWT è scaduto
    /// </summary>
    private bool IsTokenExpired(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return true;
            
        try 
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Verifica la scadenza del token
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            // In caso di errori nella verifica, considera il token scaduto
            return true;
        }
    }
}