// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\IAuthService.cs
using Kleios.Shared;
using Kleios.Shared.Models;
using System.Security.Claims;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di autenticazione
/// </summary>
public interface IAuthService
{
    Task<Option<AuthResponse>> LoginAsync(string username, string password);
    Task<Option<string>> GetSecurityStampAsync();
    Task<Option<ClaimsPrincipal>> GetUserClaims();
    Task<Option<AuthResponse>> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Ottiene un token JWT valido, effettuando il refresh se necessario
    /// </summary>
    /// <returns>Un'Option contenente il token se valido, altrimenti un errore</returns>
    Task<Option<string>> GetValidAccessTokenAsync();
    
    /// <summary>
    /// Esegue il logout dell'utente corrente
    /// </summary>
    Task LogoutAsync();
}