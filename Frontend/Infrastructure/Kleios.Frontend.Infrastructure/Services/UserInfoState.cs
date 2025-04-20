using System.Security.Claims;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Classe scoped che mantiene le informazioni dell'utente durante la sessione
/// Questo permette di avere i dati dell'utente disponibili anche durante il prerendering server
/// </summary>
public class UserInfoState
{
    /// <summary>
    /// JWT Token per l'autenticazione
    /// </summary>
    public string? AccessToken { get; private set; }
    
    /// <summary>
    /// Username dell'utente autenticato
    /// </summary>
    public string? Username { get; private set; }
    
    /// <summary>
    /// Email dell'utente autenticato
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Ruoli dell'utente autenticato
    /// </summary>
    public string[] Roles { get; private set; } = [];

    /// <summary>
    /// Claims dell'utente autenticato
    /// </summary>
    public Claim[] Claims { get; private set; } = [];
    
    /// <summary>
    /// Indica se l'utente è autenticato
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    /// <summary>
    /// Aggiorna le informazioni dell'utente con un nuovo token JWT
    /// </summary>
    public void UpdateFromToken(string accessToken, IEnumerable<Claim> claims)
    {
        AccessToken = accessToken;
        Claims = claims.ToArray();
        
        // Estrai le informazioni principali dai claims
        Username = Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        Email = Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        Roles = Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
    }
    
    /// <summary>
    /// Resetta le informazioni dell'utente (logout)
    /// </summary>
    public void Reset()
    {
        AccessToken = null;
        Username = null;
        Email = null;
        Roles = [];
        Claims = [];
    }
}