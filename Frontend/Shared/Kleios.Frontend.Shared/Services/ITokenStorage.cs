using System;
using System.Threading.Tasks;
using Kleios.Frontend.Shared.Models;
using Kleios.Shared;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per la gestione dei token di autenticazione con persistenza.
/// Consente di salvare e recuperare token JWT e refresh token associati agli utenti.
/// </summary>
public interface ITokenStorage
{
    /// <summary>
    /// Salva un token di accesso (JWT) associato a un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="accessToken">Token JWT da salvare</param>
    /// <param name="expiry">Data di scadenza del token</param>
    Task SaveAccessTokenAsync(string userId, string accessToken, DateTime expiry);
    
    /// <summary>
    /// Recupera il token di accesso (JWT) di un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>Option con le informazioni del token se trovato</returns>
    Task<Option<TokenInfo>> GetAccessTokenAsync(string userId);
    
    /// <summary>
    /// Salva un refresh token associato a un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="refreshToken">Refresh token da salvare</param>
    /// <param name="expiry">Data di scadenza del token</param>
    Task SaveRefreshTokenAsync(string userId, string refreshToken, DateTime expiry);
    
    /// <summary>
    /// Recupera il refresh token di un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>Option con le informazioni del token se trovato</returns>
    Task<Option<TokenInfo>> GetRefreshTokenAsync(string userId);
    
    /// <summary>
    /// Cancella tutti i token associati a un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    Task ClearTokensAsync(string userId);
    
    /// <summary>
    /// Verifica se esistono token validi per un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>True se esistono token validi, false altrimenti</returns>
    Task<bool> HasValidTokensAsync(string userId);
}