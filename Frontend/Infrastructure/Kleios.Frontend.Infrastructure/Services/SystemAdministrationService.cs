// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\SystemAdministrationService.cs
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Shared.Settings;
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
    
    /// <summary>
    /// Ottiene tutte le impostazioni
    /// </summary>
    public async Task<Option<IEnumerable<SettingMetadata>>> GetAllSettingsAsync()
    {
        _logger.LogInformation("Richiesta di tutte le impostazioni");
        return await _httpClient.Get<IEnumerable<SettingMetadata>>("api/settings");
    }
    
    /// <summary>
    /// Ottiene le impostazioni per categoria
    /// </summary>
    public async Task<Option<IEnumerable<SettingMetadata>>> GetSettingsByCategoryAsync(string category)
    {
        _logger.LogInformation("Richiesta impostazioni per categoria: {Category}", category);
        return await _httpClient.Get<IEnumerable<SettingMetadata>>($"api/settings/category/{category}");
    }
    
    /// <summary>
    /// Ottiene un'impostazione specifica per chiave
    /// </summary>
    public async Task<Option<SettingMetadata>> GetSettingByKeyAsync(string key)
    {
        _logger.LogInformation("Richiesta impostazione con chiave: {Key}", key);
        return await _httpClient.Get<SettingMetadata>($"api/settings/{key}");
    }
    
    /// <summary>
    /// Crea una nuova impostazione
    /// </summary>
    public async Task<Option<SettingMetadata>> CreateSettingAsync(CreateSettingRequest request)
    {
        _logger.LogInformation("Creazione nuova impostazione: {Key}", request.Key);
        return await _httpClient.PostAsJson<SettingMetadata>("api/settings", request);
    }
    
    /// <summary>
    /// Aggiorna un'impostazione esistente
    /// </summary>
    public async Task<Option<SettingMetadata>> UpdateSettingAsync(string key, UpdateSettingRequest request)
    {
        _logger.LogInformation("Aggiornamento impostazione: {Key}", key);
        return await _httpClient.PutAsJson<SettingMetadata>($"api/settings/{key}", request);
    }
    
    /// <summary>
    /// Elimina un'impostazione
    /// </summary>
    public async Task<Option<bool>> DeleteSettingAsync(string key)
    {
        _logger.LogInformation("Eliminazione impostazione: {Key}", key);
        var result = await _httpClient.Delete($"api/settings/{key}");
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }
    
    /// <summary>
    /// Ottiene i log di audit in base ai filtri specificati
    /// </summary>
    public async Task<Option<IEnumerable<AuditLogDto>>> GetAuditLogsAsync(AuditLogFilterRequest filter)
    {
        _logger.LogInformation("Richiesta log di audit con filtri");
        return await _httpClient.Get<IEnumerable<AuditLogDto>>("api/audit", filter);
    }
    
    /// <summary>
    /// Ottiene un log di audit specifico per ID
    /// </summary>
    public async Task<Option<AuditLogDto>> GetAuditLogByIdAsync(Guid id)
    {
        _logger.LogInformation("Richiesta log di audit con ID: {LogId}", id);
        return await _httpClient.Get<AuditLogDto>($"api/audit/{id}");
    }
    
    /// <summary>
    /// Ottiene i log di audit per una risorsa specifica
    /// </summary>
    public async Task<Option<IEnumerable<AuditLogDto>>> GetAuditLogsByResourceAsync(string resourceType, string resourceId)
    {
        _logger.LogInformation("Richiesta log di audit per risorsa: {ResourceType}/{ResourceId}", resourceType, resourceId);
        return await _httpClient.Get<IEnumerable<AuditLogDto>>($"api/audit/resource/{resourceType}/{resourceId}");
    }
    
    /// <summary>
    /// Ottiene i log di audit per un utente specifico
    /// </summary>
    public async Task<Option<IEnumerable<AuditLogDto>>> GetAuditLogsByUserAsync(Guid userId)
    {
        _logger.LogInformation("Richiesta log di audit per utente: {UserId}", userId);
        return await _httpClient.Get<IEnumerable<AuditLogDto>>($"api/audit/user/{userId}");
    }
    
    /// <summary>
    /// Ottiene la password policy corrente
    /// </summary>
    public async Task<Option<PasswordPolicyDto>> GetPasswordPolicyAsync()
    {
        _logger.LogInformation("Richiesta password policy");
        return await _httpClient.Get<PasswordPolicyDto>("api/passwordpolicy/policy");
    }
    
    /// <summary>
    /// Aggiorna la password policy
    /// </summary>
    public async Task<Option<PasswordPolicyDto>> UpdatePasswordPolicyAsync(PasswordPolicyDto policy)
    {
        _logger.LogInformation("Aggiornamento password policy");
        return await _httpClient.PutAsJson<PasswordPolicyDto>("api/passwordpolicy/policy", policy);
    }
    
    /// <summary>
    /// Valida una password in base alla policy corrente
    /// </summary>
    public async Task<Option<PasswordValidationResult>> ValidatePasswordAsync(string password)
    {
        _logger.LogInformation("Validazione password");
        return await _httpClient.PostAsJson<PasswordValidationResult>("api/passwordpolicy/validate", password);
    }
    
    /// <summary>
    /// Cambia la password dell'utente corrente
    /// </summary>
    public async Task<Option<bool>> ChangePasswordAsync(ChangePasswordRequest request)
    {
        _logger.LogInformation("Cambio password per utente corrente");
        var result = await _httpClient.PostAsJson<object>("api/passwordpolicy/change", request);
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }
    
    /// <summary>
    /// Forza il cambio password per un utente (solo admin)
    /// </summary>
    public async Task<Option<bool>> ForceChangePasswordAsync(Guid userId, string newPassword)
    {
        _logger.LogInformation("Reset password per utente: {UserId}", userId);
        var result = await _httpClient.PostAsJson<object>($"api/passwordpolicy/force-change/{userId}", newPassword);
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }
    
    /// <summary>
    /// Invia email per il reset della password
    /// </summary>
    public async Task<Option<bool>> ForgotPasswordAsync(ResetPasswordRequest request)
    {
        _logger.LogInformation("Richiesta reset password per email: {Email}", request.Email);
        var result = await _httpClient.PostAsJson<object>("api/passwordpolicy/forgot-password", request);
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }
    
    /// <summary>
    /// Conferma il reset della password con il token
    /// </summary>
    public async Task<Option<bool>> ResetPasswordAsync(ConfirmResetPasswordRequest request)
    {
        _logger.LogInformation("Conferma reset password");
        var result = await _httpClient.PostAsJson<object>("api/passwordpolicy/reset-password", request);
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }
    
    /// <summary>
    /// Ottiene lo stato della password per un utente
    /// </summary>
    public async Task<Option<UserPasswordStatusDto>> GetPasswordStatusAsync(Guid userId)
    {
        _logger.LogInformation("Richiesta stato password per utente: {UserId}", userId);
        return await _httpClient.Get<UserPasswordStatusDto>($"api/passwordpolicy/status/{userId}");
    }
    
    /// <summary>
    /// Ottiene gli utenti con password scadute
    /// </summary>
    public async Task<Option<IEnumerable<UserPasswordStatusDto>>> GetExpiredPasswordsAsync()
    {
        _logger.LogInformation("Richiesta password scadute");
        return await _httpClient.Get<IEnumerable<UserPasswordStatusDto>>("api/passwordpolicy/expired");
    }
    
    /// <summary>
    /// Ottiene le sessioni attive dell'utente corrente
    /// </summary>
    public async Task<Option<IEnumerable<UserSessionDto>>> GetMySessionsAsync()
    {
        _logger.LogInformation("Richiesta sessioni attive per utente corrente");
        return await _httpClient.Get<IEnumerable<UserSessionDto>>("api/session/my-sessions");
    }
    
    /// <summary>
    /// Ottiene le sessioni attive per un utente specifico
    /// </summary>
    public async Task<Option<IEnumerable<UserSessionDto>>> GetUserSessionsAsync(Guid userId)
    {
        _logger.LogInformation("Richiesta sessioni attive per utente: {UserId}", userId);
        return await _httpClient.Get<IEnumerable<UserSessionDto>>($"api/session/user/{userId}");
    }
    
    /// <summary>
    /// Termina una sessione specifica
    /// </summary>
    public async Task<Option<bool>> TerminateSessionAsync(Guid sessionId)
    {
        _logger.LogInformation("Terminazione sessione: {SessionId}", sessionId);
        var result = await _httpClient.Delete($"api/session/{sessionId}");
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }
    
    /// <summary>
    /// Termina tutte le altre sessioni dell'utente corrente
    /// </summary>
    public async Task<Option<bool>> TerminateAllOtherSessionsAsync()
    {
        _logger.LogInformation("Terminazione di tutte le altre sessioni");
        var result = await _httpClient.Delete("api/session/my-sessions/terminate-all");
        return result.IsSuccess 
            ? Option<bool>.Success(true) 
            : Option<bool>.Failure(result.Message, result.StatusCode);
    }
    
    /// <summary>
    /// Ottiene le statistiche delle sessioni per l'utente corrente
    /// </summary>
    public async Task<Option<SessionStatisticsDto>> GetMySessionStatisticsAsync()
    {
        _logger.LogInformation("Richiesta statistiche sessioni");
        return await _httpClient.Get<SessionStatisticsDto>("api/session/statistics");
    }
    
    /// <summary>
    /// Ottiene la configurazione delle sessioni
    /// </summary>
    public async Task<Option<SessionConfigurationDto>> GetSessionConfigurationAsync()
    {
        _logger.LogInformation("Richiesta configurazione sessioni");
        return await _httpClient.Get<SessionConfigurationDto>("api/session/configuration");
    }
    
    /// <summary>
    /// Aggiorna la configurazione delle sessioni
    /// </summary>
    public async Task<Option<SessionConfigurationDto>> UpdateSessionConfigurationAsync(SessionConfigurationDto config)
    {
        _logger.LogInformation("Aggiornamento configurazione sessioni");
        return await _httpClient.PutAsJson<SessionConfigurationDto>("api/session/configuration", config);
    }
}