// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\IAuthService.cs
using Kleios.Shared;
using Kleios.Shared.Models;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di autenticazione
/// </summary>
public interface IAuthService
{
    Task<Option<AuthResponse>> LoginAsync(string username, string password);
    Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Option<AuthResponse>> RefreshTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task LogoutAsync();
    Task<bool> CanNavigateToAsync(string path);
}