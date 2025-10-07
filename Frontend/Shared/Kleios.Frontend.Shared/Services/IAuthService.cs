// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\IAuthService.cs
using Kleios.Shared;
using Kleios.Shared.Models;
using System.Security.Claims;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di autenticazione frontend
/// Nota: Rinominata da IAuthService per evitare conflitti con Backend.IAuthService
/// </summary>
public interface IFrontendAuthService
{
    Task<Option<AuthResponse>> LoginAsync(string username, string password);
    Task<Option<string>> GetSecurityStampAsync();
    Task<Option<ClaimsPrincipal>> GetUserClaims(Guid userId);
    Task<Option<bool>> LogoutAsync();
}