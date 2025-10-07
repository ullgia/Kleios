// filepath: c:\Users\Giacomo\source\Kleios\Shared\Kleios.Shared\Models\UserModels.cs
namespace Kleios.Shared.Models;

/// <summary>
/// Modello per il filtro degli utenti
/// </summary>
public class UserFilter
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO per la creazione di un utente
/// </summary>
public class CreateUserRequest
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// DTO per l'aggiornamento di un utente
/// </summary>
public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Password { get; set; }
    public List<string>? Roles { get; set; }
}

/// <summary>
/// DTO per un utente
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response con i dettagli completi di un utente
/// </summary>
public class UserDetailResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsMasterUser { get; set; }
}