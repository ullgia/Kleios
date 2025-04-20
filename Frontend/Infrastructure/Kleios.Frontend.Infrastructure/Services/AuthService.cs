// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\AuthService.cs
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Frontend.Infrastructure.Helpers;
using Kleios.Frontend.Infrastructure.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio di autenticazione che utilizza service discovery di Aspire
/// e UserInfoState per funzionare anche durante prerendering
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly UserInfoState _userInfoState;
    private const string BaseEndpoint = "api/auth";

    public AuthService(
        HttpClient httpClient,
        ITokenService tokenService,
        AuthenticationStateProvider authStateProvider,
        UserInfoState userInfoState)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _authStateProvider = authStateProvider;
        _userInfoState = userInfoState;
    }

    /// <summary>
    /// Effettua il login di un utente
    /// </summary>
    public async Task<Option<AuthResponse>> LoginAsync(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var result = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/login", request);
        
        if (result.IsSuccess)
        {
            // Salva i token: access token nello UserInfoState e refresh token nel cookie
            await _tokenService.SetTokensAsync(result.Value.Token, result.Value.RefreshToken);
            
            // Aggiorna lo stato di autenticazione con il nuovo token
            if (_authStateProvider is KleiosAuthenticationStateProvider kleiosProvider)
            {
                kleiosProvider.UpdateAuthenticationState(result.Value.Token);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Registra un nuovo utente
    /// </summary>
    public async Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var result = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/register", request);
        
        if (result.IsSuccess)
        {
            // Salva i token: access token nello UserInfoState e refresh token nel cookie
            await _tokenService.SetTokensAsync(result.Value.Token, result.Value.RefreshToken);
            
            // Aggiorna lo stato di autenticazione con il nuovo token
            if (_authStateProvider is KleiosAuthenticationStateProvider kleiosProvider)
            {
                kleiosProvider.UpdateAuthenticationState(result.Value.Token);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Aggiorna il token di accesso usando il refresh token
    /// </summary>
    public async Task<Option<AuthResponse>> RefreshTokenAsync()
    {
        var refreshToken = await _tokenService.GetRefreshTokenAsync();
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Option<AuthResponse>.Failure("Refresh token non disponibile");
        }
        
        var request = new RefreshTokenRequest { RefreshToken = refreshToken };
        
        var result = await _httpClient.PostAsJson<AuthResponse>($"{BaseEndpoint}/refresh", request);
        
        if (result.IsSuccess)
        {
            // Aggiorna i token
            await _tokenService.SetTokensAsync(result.Value.Token, result.Value.RefreshToken);
            
            // Aggiorna lo stato di autenticazione con il nuovo token
            if (_authStateProvider is KleiosAuthenticationStateProvider kleiosProvider)
            {
                kleiosProvider.UpdateAuthenticationState(result.Value.Token);
            }
        }
        else
        {
            // Se c'è un errore, pulisci i token
            await LogoutAsync();
        }
        
        return result;
    }

    /// <summary>
    /// Verifica se l'utente è autenticato
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        // Prima controlla nello stato scoped (funziona sempre)
        if (_userInfoState.IsAuthenticated)
        {
            return true;
        }
        
        // Come fallback, controlla i token
        return await _tokenService.HasValidTokenAsync();
    }

    /// <summary>
    /// Effettua il logout dell'utente
    /// </summary>
    public async Task LogoutAsync()
    {
        await _tokenService.RemoveTokensAsync();
        
        // Aggiorna lo stato di autenticazione (resetta)
        if (_authStateProvider is KleiosAuthenticationStateProvider kleiosProvider)
        {
            kleiosProvider.UpdateAuthenticationState("");
        }
    }

    /// <summary>
    /// Verifica se l'utente può navigare a un certo percorso
    /// </summary>
    public async Task<bool> CanNavigateToAsync(string path)
    {
        // Percorsi pubblici che non richiedono autenticazione
        string[] publicPaths = { "/login", "/register", "/public", "/" };
        
        // Se il percorso è pubblico, consenti la navigazione
        if (publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        
        // Per tutti gli altri percorsi, verifica se l'utente è autenticato
        bool isAuthenticated = await IsAuthenticatedAsync();
        
        // Se l'utente non è autenticato, non può navigare
        if (!isAuthenticated)
        {
            return false;
        }
        
        return true;
    }
    
    // Il metodo NotifyAuthStateChanged non è più necessario perché utilizziamo
    // direttamente UpdateAuthenticationState quando necessario

    /// <summary>
    /// Recupera la lista di tutti gli utenti registrati
    /// </summary>
    /// <returns>Un'opzione contenente la lista degli utenti se l'operazione ha successo</returns>
    public async Task<Option<List<UserResponse>>> GetUsersAsync()
    {
        // Verifica se l'utente è autenticato
        if (!await IsAuthenticatedAsync())
        {
            return Option<List<UserResponse>>.Failure("Utente non autenticato");
        }
        
        try
        {
            // Effettua la richiesta al backend
            var result = await _httpClient.Get<List<UserResponse>>($"{BaseEndpoint}/users");
            return result;
        }
        catch (Exception ex)
        {
            return Option<List<UserResponse>>.Failure($"Errore durante il recupero degli utenti: {ex.Message}");
        }
    }
}

