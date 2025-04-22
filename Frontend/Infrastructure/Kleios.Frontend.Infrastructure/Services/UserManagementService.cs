// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\UserManagementService.cs
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Frontend.Infrastructure.Helpers;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio di gestione utenti che utilizza service discovery di Aspire
/// e HttpClientHelper per semplificare le richieste
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly HttpClient _httpClient;
    private const string UsersEndpoint = "api/users";
    private const string RolesEndpoint = "api/roles";
    private const string PermissionsEndpoint = "api/permissions";

    public UserManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

 

    /// <summary>
    /// Ottiene un utente specifico per ID
    /// </summary>
    public async Task<Option<UserDto>> GetUserByIdAsync(Guid id)
    {
        return await _httpClient.Get<UserDto>($"{UsersEndpoint}/{id}");
    }

    /// <summary>
    /// Crea un nuovo utente
    /// </summary>
    public async Task<Option<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        return await _httpClient.PostAsJson<UserDto>(UsersEndpoint, request);
    }

    /// <summary>
    /// Aggiorna un utente esistente
    /// </summary>
    public async Task<Option<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        return await _httpClient.PutAsJson<UserDto>($"{UsersEndpoint}/{id}", request);
    }

    /// <summary>
    /// Elimina un utente
    /// </summary>
    public async Task<Option<bool>> DeleteUserAsync(Guid id)
    {
        // Eseguiamo la richiesta DELETE e convertiamo il risultato
        var result = await _httpClient.Delete($"{UsersEndpoint}/{id}");
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }

    /// <summary>
    /// Ottiene tutti i ruoli disponibili
    /// </summary>
    public async Task<Option<IEnumerable<RoleDto>>> GetRolesAsync()
    {
        return await _httpClient.Get<IEnumerable<RoleDto>>(RolesEndpoint);
    }

    /// <summary>
    /// Crea un nuovo ruolo
    /// </summary>
    public async Task<Option<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        return await _httpClient.PostAsJson<RoleDto>(RolesEndpoint, request);
    }

    /// <summary>
    /// Aggiorna un ruolo esistente
    /// </summary>
    public async Task<Option<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleRequest request)
    {
        return await _httpClient.PutAsJson<RoleDto>($"{RolesEndpoint}/{id}", request);
    }

    /// <summary>
    /// Elimina un ruolo
    /// </summary>
    public async Task<Option<bool>> DeleteRoleAsync(Guid id)
    {
        var result = await _httpClient.Delete($"{RolesEndpoint}/{id}");
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }

    /// <summary>
    /// Ottiene tutti i permessi disponibili
    /// </summary>
    public async Task<Option<IEnumerable<PermissionDto>>> GetPermissionsAsync()
    {
        return await _httpClient.Get<IEnumerable<PermissionDto>>(PermissionsEndpoint);
    }
}