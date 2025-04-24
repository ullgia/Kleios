// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\SystemAdministrationService.cs
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Frontend.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio di amministrazione di sistema che si interfaccia con Kleios.Backend.SystemAdmin
/// per la gestione degli utenti, dei ruoli e di altre funzionalità amministrative
/// </summary>
public class SystemAdministrationService : ISystemAdministrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SystemAdministrationService> _logger;
    
    private const string AuthEndpoint = "api/auth";
    private const string UsersEndpoint = "api/users";
    private const string RolesEndpoint = "api/roles";
    private const string PermissionsEndpoint = "api/permissions";

    public SystemAdministrationService(
        HttpClient httpClient,
        ILogger<SystemAdministrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuovo utente
    /// </summary>
    public async Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            _logger.LogWarning("Tentativo di registrazione con dati incompleti");
            return Option<AuthResponse>.ValidationError("I dati di registrazione sono incompleti");
        }

        _logger.LogInformation("Tentativo di registrazione per utente: {Username}", request.Username);

        var result = await _httpClient.PostAsJson<AuthResponse>($"{AuthEndpoint}/register", request);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Registrazione completata con successo per l'utente: {Username}", request.Username);
        }
        else
        {
            _logger.LogWarning("Registrazione fallita per l'utente {Username}: {Error}", request.Username, result.Message);
        }
        
        return result;
    }

    /// <summary>
    /// Ottiene gli utenti in base ai filtri specificati
    /// </summary>
    public async Task<Option<IEnumerable<UserDto>>> GetUsersAsync(UserFilter filter)
    {
        _logger.LogInformation("Richiesta degli utenti con filtro");
        return await _httpClient.Get<IEnumerable<UserDto>>(UsersEndpoint, filter);
    }

    /// <summary>
    /// Ottiene un utente specifico per ID
    /// </summary>
    public async Task<Option<UserDto>> GetUserByIdAsync(Guid id)
    {
        _logger.LogInformation("Richiesta dell'utente con ID: {UserId}", id);
        return await _httpClient.Get<UserDto>($"{UsersEndpoint}/{id}");
    }

    /// <summary>
    /// Crea un nuovo utente
    /// </summary>
    public async Task<Option<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        _logger.LogInformation("Creazione di un nuovo utente: {Username}", request.Username);
        return await _httpClient.PostAsJson<UserDto>(UsersEndpoint, request);
    }

    /// <summary>
    /// Aggiorna un utente esistente
    /// </summary>
    public async Task<Option<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        _logger.LogInformation("Aggiornamento dell'utente con ID: {UserId}", id);
        return await _httpClient.PutAsJson<UserDto>($"{UsersEndpoint}/{id}", request);
    }

    /// <summary>
    /// Elimina un utente
    /// </summary>
    public async Task<Option<bool>> DeleteUserAsync(Guid id)
    {
        _logger.LogInformation("Eliminazione dell'utente con ID: {UserId}", id);
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
        _logger.LogInformation("Richiesta di tutti i ruoli");
        return await _httpClient.Get<IEnumerable<RoleDto>>(RolesEndpoint);
    }

    /// <summary>
    /// Crea un nuovo ruolo
    /// </summary>
    public async Task<Option<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        _logger.LogInformation("Creazione di un nuovo ruolo: {RoleName}", request.Name);
        return await _httpClient.PostAsJson<RoleDto>(RolesEndpoint, request);
    }

    /// <summary>
    /// Aggiorna un ruolo esistente
    /// </summary>
    public async Task<Option<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleRequest request)
    {
        _logger.LogInformation("Aggiornamento del ruolo con ID: {RoleId}", id);
        return await _httpClient.PutAsJson<RoleDto>($"{RolesEndpoint}/{id}", request);
    }

    /// <summary>
    /// Elimina un ruolo
    /// </summary>
    public async Task<Option<bool>> DeleteRoleAsync(Guid id)
    {
        _logger.LogInformation("Eliminazione del ruolo con ID: {RoleId}", id);
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
        _logger.LogInformation("Richiesta di tutti i permessi");
        return await _httpClient.Get<IEnumerable<PermissionDto>>(PermissionsEndpoint);
    }
}