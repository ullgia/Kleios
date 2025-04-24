// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ISystemAdministrationService.cs
using Kleios.Shared;
using Kleios.Shared.Models;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di amministrazione di sistema
/// Gestisce utenti, ruoli e altre funzionalità di amministrazione
/// </summary>
public interface ISystemAdministrationService
{
    /// <summary>
    /// Registra un nuovo utente
    /// </summary>
    Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request);
    
    /// <summary>
    /// Ottiene gli utenti in base ai filtri specificati
    /// </summary>
    Task<Option<IEnumerable<UserDto>>> GetUsersAsync(UserFilter filter);
    
    /// <summary>
    /// Ottiene un utente specifico per ID
    /// </summary>
    Task<Option<UserDto>> GetUserByIdAsync(Guid id);
    
    /// <summary>
    /// Crea un nuovo utente
    /// </summary>
    Task<Option<UserDto>> CreateUserAsync(CreateUserRequest request);
    
    /// <summary>
    /// Aggiorna un utente esistente
    /// </summary>
    Task<Option<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request);
    
    /// <summary>
    /// Elimina un utente
    /// </summary>
    Task<Option<bool>> DeleteUserAsync(Guid id);
    
    /// <summary>
    /// Ottiene tutti i ruoli disponibili
    /// </summary>
    Task<Option<IEnumerable<RoleDto>>> GetRolesAsync();
    
    /// <summary>
    /// Crea un nuovo ruolo
    /// </summary>
    Task<Option<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
    
    /// <summary>
    /// Aggiorna un ruolo esistente
    /// </summary>
    Task<Option<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleRequest request);
    
    /// <summary>
    /// Elimina un ruolo
    /// </summary>
    Task<Option<bool>> DeleteRoleAsync(Guid id);
    
    /// <summary>
    /// Ottiene tutti i permessi disponibili
    /// </summary>
    Task<Option<IEnumerable<PermissionDto>>> GetPermissionsAsync();
}