using Kleios.Shared;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Servizio per gestire l'autenticazione degli utenti
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Effettua login con email e password
    /// </summary>
    Task<Option<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Effettua logout e rimuove il cookie
    /// </summary>
    Task<Option> LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene le informazioni dell'utente corrente
    /// </summary>
    Task<Option<UserInfo>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida il token corrente
    /// </summary>
    Task<Option<bool>> ValidateTokenAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Request per login
/// </summary>
public record LoginRequest(string Email, string Password, bool RememberMe = false);

/// <summary>
/// Response da login
/// </summary>
public record LoginResponse(string Token, string Email, string[] Roles);

/// <summary>
/// Informazioni utente
/// </summary>
public record UserInfo(string Id, string Email, string[] Roles, Dictionary<string, string>? AdditionalClaims = null);
