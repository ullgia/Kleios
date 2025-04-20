// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ITokenService.cs
namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio che gestisce i token di autenticazione
/// </summary>
public interface ITokenService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string refreshToken);
    Task RemoveTokensAsync();
    Task<bool> HasValidTokenAsync();
}