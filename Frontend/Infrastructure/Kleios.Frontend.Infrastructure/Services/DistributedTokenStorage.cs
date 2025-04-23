using System;
using System.Text.Json;
using System.Threading.Tasks;
using Kleios.Frontend.Shared.Models;
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione di ITokenStorage che utilizza una cache distribuita con fallback su localStorage
/// </summary>
public class DistributedTokenStorage : ITokenStorage
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedTokenStorage> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _jsRuntime;
    
    private const string AccessTokenType = "access_token";
    private const string RefreshTokenType = "refresh_token";
    private const string LocalStorageKeyPrefix = "kleios_token_storage_";
    
    // Prefissi per le chiavi nella cache
    private const string CacheKeyPrefix = "Kleios:Tokens:";

    public DistributedTokenStorage(
        IDistributedCache cache,
        IHttpContextAccessor httpContextAccessor,
        IJSRuntime jsRuntime,
        ILogger<DistributedTokenStorage> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Salva un token di accesso (JWT) associato a un utente
    /// </summary>
    public async Task SaveAccessTokenAsync(string userId, string accessToken, DateTime expiry)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("Tentativo di salvare un access token nullo o vuoto");
            return;
        }
        
        try
        {
            var tokenInfo = new TokenInfo
            {
                Token = accessToken,
                Expiry = expiry,
                TokenType = AccessTokenType,
                DeviceId = GetDeviceId(),
                CreatedAt = DateTime.UtcNow
            };
            
            // Salva nella cache distribuita
            await SaveToCacheAsync($"{CacheKeyPrefix}{userId}:{AccessTokenType}:{GetDeviceId()}", tokenInfo, expiry);
            
            // Backup nel localStorage (solo se siamo in un contesto browser)
            if (IsClientSideContext())
            {
                await SaveToLocalStorageAsync($"{LocalStorageKeyPrefix}{userId}:{AccessTokenType}", tokenInfo);
            }
            
            _logger.LogDebug("Access token salvato con successo per l'utente {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio dell'access token per l'utente {UserId}", userId);
        }
    }

    /// <summary>
    /// Recupera il token di accesso (JWT) di un utente
    /// </summary>
    public async Task<Option<TokenInfo>> GetAccessTokenAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Tentativo di recuperare un access token per un utente nullo o vuoto");
            return Option<TokenInfo>.ValidationError("ID utente non valido");
        }
        
        try
        {
            // Tenta di recuperare dalla cache distribuita
            var tokenInfo = await GetFromCacheAsync<TokenInfo>($"{CacheKeyPrefix}{userId}:{AccessTokenType}:{GetDeviceId()}");
            
            // Se non trovato in cache e siamo in un contesto browser, prova dal localStorage
            if (tokenInfo == null && IsClientSideContext())
            {
                tokenInfo = await GetFromLocalStorageAsync<TokenInfo>($"{LocalStorageKeyPrefix}{userId}:{AccessTokenType}");
                
                // Se trovato nel localStorage, ricarica anche nella cache per le future richieste
                if (tokenInfo != null && tokenInfo.Expiry > DateTime.UtcNow)
                {
                    await SaveToCacheAsync($"{CacheKeyPrefix}{userId}:{AccessTokenType}:{GetDeviceId()}", tokenInfo, tokenInfo.Expiry);
                }
            }
            
            if (tokenInfo != null && tokenInfo.Expiry > DateTime.UtcNow)
            {
                _logger.LogDebug("Access token recuperato con successo per l'utente {UserId}", userId);
                return Option<TokenInfo>.Success(tokenInfo);
            }
            
            _logger.LogDebug("Nessun access token valido trovato per l'utente {UserId}", userId);
            return Option<TokenInfo>.NotFound("Nessun token valido trovato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dell'access token per l'utente {UserId}", userId);
            return Option<TokenInfo>.ServerError($"Errore nel recupero del token: {ex.Message}");
        }
    }

    /// <summary>
    /// Salva un refresh token associato a un utente
    /// </summary>
    public async Task SaveRefreshTokenAsync(string userId, string refreshToken, DateTime expiry)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Tentativo di salvare un refresh token nullo o vuoto");
            return;
        }
        
        try
        {
            var tokenInfo = new TokenInfo
            {
                Token = refreshToken,
                Expiry = expiry,
                TokenType = RefreshTokenType,
                DeviceId = GetDeviceId(),
                CreatedAt = DateTime.UtcNow
            };
            
            // Salva nella cache distribuita
            await SaveToCacheAsync($"{CacheKeyPrefix}{userId}:{RefreshTokenType}:{GetDeviceId()}", tokenInfo, expiry);
            
            // Backup nel localStorage (solo se siamo in un contesto browser)
            if (IsClientSideContext())
            {
                await SaveToLocalStorageAsync($"{LocalStorageKeyPrefix}{userId}:{RefreshTokenType}", tokenInfo);
            }
            
            // Salva anche nel cookie per il fallback completo
            SaveRefreshTokenInCookie(userId, refreshToken, expiry);
            
            _logger.LogDebug("Refresh token salvato con successo per l'utente {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio del refresh token per l'utente {UserId}", userId);
        }
    }

    /// <summary>
    /// Recupera il refresh token di un utente
    /// </summary>
    public async Task<Option<TokenInfo>> GetRefreshTokenAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Tentativo di recuperare un refresh token per un utente nullo o vuoto");
            return Option<TokenInfo>.ValidationError("ID utente non valido");
        }
        
        try
        {
            // Tenta di recuperare dalla cache distribuita
            var tokenInfo = await GetFromCacheAsync<TokenInfo>($"{CacheKeyPrefix}{userId}:{RefreshTokenType}:{GetDeviceId()}");
            
            // Se non trovato in cache, controlla altre fonti
            if (tokenInfo == null)
            {
                // Prova dal localStorage se siamo in un contesto browser
                if (IsClientSideContext())
                {
                    tokenInfo = await GetFromLocalStorageAsync<TokenInfo>($"{LocalStorageKeyPrefix}{userId}:{RefreshTokenType}");
                }
                
                // Ultimo tentativo: recupera dal cookie
                if (tokenInfo == null)
                {
                    var cookieToken = GetRefreshTokenFromCookie(userId);
                    if (!string.IsNullOrEmpty(cookieToken.Token))
                    {
                        tokenInfo = new TokenInfo
                        {
                            Token = cookieToken.Token,
                            Expiry = cookieToken.Expiry,
                            TokenType = RefreshTokenType,
                            DeviceId = GetDeviceId(),
                            CreatedAt = DateTime.UtcNow.AddDays(-1) // Supponiamo che sia stato creato ieri
                        };
                        
                        // Salva in cache per le future richieste
                        await SaveToCacheAsync($"{CacheKeyPrefix}{userId}:{RefreshTokenType}:{GetDeviceId()}", tokenInfo, cookieToken.Expiry);
                        
                        if (IsClientSideContext())
                        {
                            await SaveToLocalStorageAsync($"{LocalStorageKeyPrefix}{userId}:{RefreshTokenType}", tokenInfo);
                        }
                    }
                }
            }
            
            if (tokenInfo != null && tokenInfo.Expiry > DateTime.UtcNow)
            {
                _logger.LogDebug("Refresh token recuperato con successo per l'utente {UserId}", userId);
                return Option<TokenInfo>.Success(tokenInfo);
            }
            
            _logger.LogDebug("Nessun refresh token valido trovato per l'utente {UserId}", userId);
            return Option<TokenInfo>.NotFound("Nessun token valido trovato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del refresh token per l'utente {UserId}", userId);
            return Option<TokenInfo>.ServerError($"Errore nel recupero del token: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancella tutti i token associati a un utente
    /// </summary>
    public async Task ClearTokensAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Tentativo di cancellare token per un utente nullo o vuoto");
            return;
        }
        
        try
        {
            // Rimuovi dalla cache distribuita
            await _cache.RemoveAsync($"{CacheKeyPrefix}{userId}:{AccessTokenType}:{GetDeviceId()}");
            await _cache.RemoveAsync($"{CacheKeyPrefix}{userId}:{RefreshTokenType}:{GetDeviceId()}");
            
            // Rimuovi dal localStorage se siamo in un contesto browser
            if (IsClientSideContext())
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"{LocalStorageKeyPrefix}{userId}:{AccessTokenType}");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"{LocalStorageKeyPrefix}{userId}:{RefreshTokenType}");
            }
            
            // Rimuovi il cookie
            ClearRefreshTokenCookie(userId);
            
            _logger.LogDebug("Token cancellati con successo per l'utente {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la cancellazione dei token per l'utente {UserId}", userId);
        }
    }

    /// <summary>
    /// Verifica se esistono token validi per un utente
    /// </summary>
    public async Task<bool> HasValidTokensAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Tentativo di verificare token per un utente nullo o vuoto");
            return false;
        }
        
        try
        {
            var accessToken = await GetAccessTokenAsync(userId);
            var refreshToken = await GetRefreshTokenAsync(userId);
            
            return accessToken.IsSuccess && refreshToken.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la verifica dei token per l'utente {UserId}", userId);
            return false;
        }
    }

    #region Helper methods
    
    /// <summary>
    /// Verifica se ci troviamo in un contesto client (browser)
    /// </summary>
    private bool IsClientSideContext()
    {
        try
        {
            // Se possiamo accedere a JSRuntime senza eccezioni, siamo in un contesto browser
            return _jsRuntime != null;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Genera un identificativo del dispositivo/browser corrente
    /// </summary>
    private string GetDeviceId()
    {
        // In una implementazione reale, potremmo generare un ID basato su user-agent, IP, ecc.
        // Per semplicità, generiamo un valore casuale o usiamo uno fisso
        
        // TODO: Implementare una logica più solida per identificare il dispositivo
        return "default-device";
    }
    
    /// <summary>
    /// Salva un oggetto nella cache distribuita
    /// </summary>
    private async Task SaveToCacheAsync<T>(string key, T value, DateTime expiry)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiry
        };
        
        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, json, options);
    }
    
    /// <summary>
    /// Recupera un oggetto dalla cache distribuita
    /// </summary>
    private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
    {
        var json = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Salva un oggetto nel localStorage del browser
    /// </summary>
    private async Task SaveToLocalStorageAsync<T>(string key, T value)
    {
        if (!IsClientSideContext())
        {
            return;
        }
        
        var json = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }
    
    /// <summary>
    /// Recupera un oggetto dal localStorage del browser
    /// </summary>
    private async Task<T?> GetFromLocalStorageAsync<T>(string key) where T : class
    {
        if (!IsClientSideContext())
        {
            return null;
        }
        
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Salva il refresh token in un cookie HTTP-only
    /// </summary>
    private void SaveRefreshTokenInCookie(string userId, string refreshToken, DateTime expiry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiry
            };
            
            httpContext.Response.Cookies.Append($"refresh_token_{userId}", refreshToken, cookieOptions);
            _logger.LogDebug("Refresh token salvato nel cookie con scadenza: {Expires}", cookieOptions.Expires);
        }
        else
        {
            _logger.LogWarning("HttpContext non disponibile per salvare il cookie");
        }
    }
    
    /// <summary>
    /// Recupera il refresh token da un cookie HTTP-only
    /// </summary>
    private TokenInfo GetRefreshTokenFromCookie(string userId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Cookies.TryGetValue($"refresh_token_{userId}", out var refreshToken))
        {
            // Non abbiamo un modo affidabile per sapere quando scade il cookie dal server
            // quindi usiamo una data ragionevole (7 giorni dal momento attuale)
            return new TokenInfo
            {
                Token = refreshToken,
                Expiry = DateTime.UtcNow.AddDays(7),
                TokenType = RefreshTokenType,
                DeviceId = GetDeviceId(),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }
        
        return new TokenInfo
        {
            Token = string.Empty,
            Expiry = DateTime.MinValue
        };
    }
    
    /// <summary>
    /// Cancella il cookie del refresh token
    /// </summary>
    private void ClearRefreshTokenCookie(string userId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Delete($"refresh_token_{userId}");
            _logger.LogDebug("Cookie del refresh token rimosso");
        }
        else
        {
            _logger.LogWarning("HttpContext non disponibile per rimuovere il cookie");
        }
    }
    
    #endregion
}