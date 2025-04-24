// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ITokenRefreshService.cs
using Kleios.Shared;
using Kleios.Shared.Models;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di refresh dei token.
/// Questa interfaccia risolve la dipendenza circolare tra IAuthService e ITokenDistributionService
/// fornendo un servizio dedicato esclusivamente al refresh dei token.
/// </summary>
public interface ITokenRefreshService
{
    /// <summary>
    /// Effettua il refresh di un token JWT usando un refresh token
    /// </summary>
    /// <param name="refreshToken">Il refresh token da utilizzare</param>
    /// <returns>Un'Option che contiene la risposta dell'autenticazione se il refresh ha successo</returns>
    Task<Option<AuthResponse>> RefreshTokenAsync(string refreshToken);
}