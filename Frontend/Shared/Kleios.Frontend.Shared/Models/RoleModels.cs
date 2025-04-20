// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Models\RoleModels.cs
namespace Kleios.Frontend.Shared.Models;

/// <summary>
/// DTO per la creazione di un ruolo
/// </summary>
public class CreateRoleRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<Guid> Permissions { get; set; } = new List<Guid>();
}

/// <summary>
/// DTO per l'aggiornamento di un ruolo
/// </summary>
public class UpdateRoleRequest
{
    public string? Description { get; set; }
    public List<Guid>? Permissions { get; set; }
}

/// <summary>
/// DTO per un ruolo
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO per un permesso
/// </summary>
public class PermissionDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string SystemName { get; set; }
    public string Description { get; set; } = string.Empty;
}