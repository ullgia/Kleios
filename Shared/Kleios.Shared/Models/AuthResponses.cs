namespace Kleios.Shared.Models;

/// <summary>
/// Modello per la risposta di autenticazione
/// </summary>
public class AuthResponse
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }  // Token per il refresh dell'autenticazione
    public required Guid UserId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; } // Kept for backward compatibility
    public List<string> Roles { get; set; } = new List<string>(); // New property for multiple roles
    public DateTime Expiration { get; set; }
    public string SecurityStamp { get; set; } = string.Empty; // Stamp di sicurezza per la validazione del token
}

/// <summary>
/// Modello per la risposta contenente informazioni su un utente
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}