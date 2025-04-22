using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kleios.Backend.SystemAdmin.Services;

public interface IRoleService
{
    Task<Option<IEnumerable<ApplicationRole>>> GetAllRolesAsync();
    Task<Option<ApplicationRole>> GetRoleByIdAsync(Guid id);
    Task<Option<ApplicationRole>> CreateRoleAsync(string name, string description, bool isSystemRole, IEnumerable<string> permissions);
    Task<Option<ApplicationRole>> UpdateRoleAsync(Guid id, string? name, string? description, bool? isSystemRole, IEnumerable<string>? permissions);
    Task<Option> DeleteRoleAsync(Guid id);
    Task<Option<IEnumerable<Permission>>> GetAllPermissionsAsync();
}

/// <summary>
/// Service for role and permission management operations
/// </summary>
public class RoleService : IRoleService
{
    private readonly KleiosDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleService(
        KleiosDbContext context,
        RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<Option<IEnumerable<ApplicationRole>>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return Option<IEnumerable<ApplicationRole>>.Success(roles);
    }

    public async Task<Option<ApplicationRole>> GetRoleByIdAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            return Option<ApplicationRole>.NotFound("Ruolo non trovato");
        }

        return Option<ApplicationRole>.Success(role);
    }

    public async Task<Option<ApplicationRole>> CreateRoleAsync(string name, string description, bool isSystemRole, IEnumerable<string> permissions)
    {
        var roleExists = await _roleManager.RoleExistsAsync(name);
        if (roleExists)
        {
            return Option<ApplicationRole>.Conflict($"Il ruolo '{name}' esiste già");
        }

        var role = new ApplicationRole
        {
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return Option<ApplicationRole>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (permissions.Any())
        {
            await AssignPermissionsToRole(role.Id, permissions);
        }

        return Option<ApplicationRole>.Success(role);
    }

    public async Task<Option<ApplicationRole>> UpdateRoleAsync(Guid id, string? name, string? description, bool? isSystemRole, IEnumerable<string>? permissions)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            return Option<ApplicationRole>.NotFound("Ruolo non trovato");
        }

        if (!string.IsNullOrEmpty(name) && name != role.Name)
        {
            var roleExists = await _roleManager.RoleExistsAsync(name);
            if (roleExists)
            {
                return Option<ApplicationRole>.Conflict($"Il ruolo '{name}' esiste già");
            }
            
            role.Name = name;
        }

        if (!string.IsNullOrEmpty(description))
        {
            role.Description = description;
        }

        if (isSystemRole.HasValue)
        {
            role.IsSystemRole = isSystemRole.Value;
        }

        role.UpdatedAt = DateTime.UtcNow;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return Option<ApplicationRole>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (permissions != null && permissions.Any())
        {
            await AssignPermissionsToRole(role.Id, permissions);
        }

        return Option<ApplicationRole>.Success(role);
    }

    public async Task<Option> DeleteRoleAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            return Option.NotFound("Ruolo non trovato");
        }

        if (role.IsSystemRole)
        {
            return Option.Forbidden("I ruoli di sistema non possono essere eliminati");
        }
        
        // Elimina le associazioni ruolo-permessi
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .ToListAsync();
        
        _context.RolePermissions.RemoveRange(rolePermissions);
        await _context.SaveChangesAsync();

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return Option.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Option.Success();
    }

    public async Task<Option<IEnumerable<Permission>>> GetAllPermissionsAsync()
    {
        var permissions = await _context.Permissions.ToListAsync();
        return Option<IEnumerable<Permission>>.Success(permissions);
    }

    private async Task AssignPermissionsToRole(Guid roleId, IEnumerable<string> permissions)
    {
        // Rimuovi i permessi esistenti
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
        
        _context.RolePermissions.RemoveRange(existingPermissions);
        
        // Aggiungi i nuovi permessi
        foreach (var permissionName in permissions)
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permissionName);
            
            if (permission != null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permission.Id
                });
            }
        }
        
        await _context.SaveChangesAsync();
    }
}