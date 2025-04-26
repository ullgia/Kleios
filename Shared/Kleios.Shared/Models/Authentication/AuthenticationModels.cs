namespace Kleios.Shared.Models.Authentication;

/// <summary>
/// Modello per la richiesta di login
/// </summary>
public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
}

/// <summary>
/// Modello per la richiesta di registrazione
/// </summary>
public class RegisterRequest
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
}

/// <summary>
/// Modello per la richiesta di cambio password
/// </summary>
public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}

/// <summary>
/// Modello per la risposta di autenticazione
/// </summary>
public class AuthResponse
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime Expiration { get; set; }
    public required string Username { get; set; }
    public required IEnumerable<string> Roles { get; set; }
}