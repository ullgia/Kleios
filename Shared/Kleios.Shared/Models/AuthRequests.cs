namespace Kleios.Shared.Models;

/// <summary>
/// Modello per la richiesta di login
/// </summary>
public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

/// <summary>
/// Modello per la richiesta di registrazione
/// </summary>
public class RegisterRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}