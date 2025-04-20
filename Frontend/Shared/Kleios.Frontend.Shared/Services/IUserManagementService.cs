// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\IUserManagementService.cs
using Kleios.Shared;
using Kleios.Shared.Models;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di gestione utenti
/// </summary>
public interface IUserManagementService
{
    Task<Option<IEnumerable<UserDto>>> GetUsersAsync(UserFilter filter);
    Task<Option<UserDto>> GetUserByIdAsync(Guid id);
    Task<Option<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<Option<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<Option<bool>> DeleteUserAsync(Guid id);
    Task<Option<IEnumerable<RoleDto>>> GetRolesAsync();
    Task<Option<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
    Task<Option<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleRequest request);
    Task<Option<bool>> DeleteRoleAsync(Guid id);
    Task<Option<IEnumerable<PermissionDto>>> GetPermissionsAsync();
}