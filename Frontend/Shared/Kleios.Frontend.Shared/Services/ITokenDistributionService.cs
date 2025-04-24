// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ITokenDistributionService.cs
using Kleios.Shared;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di distribuzione dei token JWT
/// che supporta sia server rendering che altre modalità
/// </summary>
public interface ITokenDistributionService
{
    /// <summary>
    /// Ottiene un token JWT valido per l'utente specificato
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="circuitId">ID del circuito Blazor (se in server rendering)</param>
    /// <returns>Un token JWT valido o un errore</returns>
    Task<Option<string>> GetValidTokenAsync(Guid userId, string? circuitId = null);
    
    /// <summary>
    /// Salva i token dopo il login o la registrazione
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="token">Token JWT</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="circuitId">ID del circuito Blazor (se in server rendering)</param>
    Task SaveTokensAsync(Guid userId, string token, string refreshToken, string? circuitId = null);
    
    /// <summary>
    /// Invalida i token dell'utente (logout)
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="circuitId">ID del circuito Blazor (se in server rendering)</param>
    Task InvalidateTokensAsync(Guid userId, string? circuitId = null);
}