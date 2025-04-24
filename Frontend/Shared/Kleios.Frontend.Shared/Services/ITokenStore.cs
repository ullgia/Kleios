// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ITokenStore.cs
namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia che definisce un meccanismo di archiviazione per i token JWT e refresh token.
/// L'implementazione concreta gestisce i dettagli di come i token vengono salvati e recuperati.
/// </summary>
public interface ITokenStore
{
    /// <summary>
    /// Salva un token JWT e il relativo refresh token per un utente specifico
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="jwtToken">Token JWT</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="context">Contesto opzionale (es. ID circuito per server rendering)</param>
    Task SaveTokensAsync(Guid userId, string jwtToken, string refreshToken, string? context = null);
    
    /// <summary>
    /// Recupera il token JWT per un utente specifico
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="context">Contesto opzionale (es. ID circuito per server rendering)</param>
    /// <returns>Il token JWT o null se non trovato</returns>
    Task<string?> GetJwtTokenAsync(Guid userId, string? context = null);
    
    /// <summary>
    /// Recupera il refresh token per un utente specifico
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="context">Contesto opzionale (es. ID circuito per server rendering)</param>
    /// <returns>Il refresh token o null se non trovato</returns>
    Task<string?> GetRefreshTokenAsync(Guid userId, string? context = null);
    
    /// <summary>
    /// Rimuove tutti i token per un utente specifico
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="context">Contesto opzionale (es. ID circuito per server rendering)</param>
    Task RemoveTokensAsync(Guid userId, string? context = null);
    
    /// <summary>
    /// Aggiorna un token JWT esistente per un utente specifico
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="jwtToken">Nuovo token JWT</param>
    /// <param name="context">Contesto opzionale (es. ID circuito per server rendering)</param>
    Task UpdateJwtTokenAsync(Guid userId, string jwtToken, string? context = null);
    
    /// <summary>
    /// Aggiorna un refresh token esistente per un utente specifico
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="refreshToken">Nuovo refresh token</param>
    /// <param name="context">Contesto opzionale (es. ID circuito per server rendering)</param>
    Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, string? context = null);
}