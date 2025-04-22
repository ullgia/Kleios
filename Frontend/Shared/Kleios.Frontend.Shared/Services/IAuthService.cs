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
    Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Option<string>> GetSecurityStampAsync();
    Task<Option<ClaimsPrincipal>> GetUserClaims();
}