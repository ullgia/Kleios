// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ISystemAdministrationService.cs
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Shared.Settings;

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
    
    /// <summary>
    /// Ottiene tutte le impostazioni
    /// </summary>
    Task<Option<IEnumerable<SettingMetadata>>> GetAllSettingsAsync();
    
    /// <summary>
    /// Ottiene le impostazioni per categoria
    /// </summary>
    Task<Option<IEnumerable<SettingMetadata>>> GetSettingsByCategoryAsync(string category);
    
    /// <summary>
    /// Ottiene un'impostazione specifica per chiave
    /// </summary>
    Task<Option<SettingMetadata>> GetSettingByKeyAsync(string key);
    
    /// <summary>
    /// Crea una nuova impostazione
    /// </summary>
    Task<Option<SettingMetadata>> CreateSettingAsync(CreateSettingRequest request);
    
    /// <summary>
    /// Aggiorna un'impostazione esistente
    /// </summary>
    Task<Option<SettingMetadata>> UpdateSettingAsync(string key, UpdateSettingRequest request);
    
    /// <summary>
    /// Elimina un'impostazione
    /// </summary>
    Task<Option<bool>> DeleteSettingAsync(string key);
    
    /// <summary>
    /// Ottiene i log di audit in base ai filtri specificati
    /// </summary>
    Task<Option<IEnumerable<AuditLogDto>>> GetAuditLogsAsync(AuditLogFilterRequest filter);
    
    /// <summary>
    /// Ottiene un log di audit specifico per ID
    /// </summary>
    Task<Option<AuditLogDto>> GetAuditLogByIdAsync(Guid id);
    
    /// <summary>
    /// Ottiene i log di audit per una risorsa specifica
    /// </summary>
    Task<Option<IEnumerable<AuditLogDto>>> GetAuditLogsByResourceAsync(string resourceType, string resourceId);
    
    /// <summary>
    /// Ottiene i log di audit per un utente specifico
    /// </summary>
    Task<Option<IEnumerable<AuditLogDto>>> GetAuditLogsByUserAsync(Guid userId);
    
    /// <summary>
    /// Ottiene la password policy corrente
    /// </summary>
    Task<Option<PasswordPolicyDto>> GetPasswordPolicyAsync();
    
    /// <summary>
    /// Aggiorna la password policy
    /// </summary>
    Task<Option<PasswordPolicyDto>> UpdatePasswordPolicyAsync(PasswordPolicyDto policy);
    
    /// <summary>
    /// Valida una password in base alla policy corrente
    /// </summary>
    Task<Option<PasswordValidationResult>> ValidatePasswordAsync(string password);
    
    /// <summary>
    /// Cambia la password dell'utente corrente
    /// </summary>
    Task<Option<bool>> ChangePasswordAsync(ChangePasswordRequest request);
    
    /// <summary>
    /// Forza il cambio password per un utente (solo admin)
    /// </summary>
    Task<Option<bool>> ForceChangePasswordAsync(Guid userId, string newPassword);
    
    /// <summary>
    /// Invia email per il reset della password
    /// </summary>
    Task<Option<bool>> ForgotPasswordAsync(ResetPasswordRequest request);
    
    /// <summary>
    /// Conferma il reset della password con il token
    /// </summary>
    Task<Option<bool>> ResetPasswordAsync(ConfirmResetPasswordRequest request);
    
    /// <summary>
    /// Ottiene lo stato della password per un utente
    /// </summary>
    Task<Option<UserPasswordStatusDto>> GetPasswordStatusAsync(Guid userId);
    
    /// <summary>
    /// Ottiene gli utenti con password scadute
    /// </summary>
    Task<Option<IEnumerable<UserPasswordStatusDto>>> GetExpiredPasswordsAsync();
    
    /// <summary>
    /// Ottiene le sessioni attive dell'utente corrente
    /// </summary>
    Task<Option<IEnumerable<UserSessionDto>>> GetMySessionsAsync();
    
    /// <summary>
    /// Ottiene le sessioni attive per un utente specifico
    /// </summary>
    Task<Option<IEnumerable<UserSessionDto>>> GetUserSessionsAsync(Guid userId);
    
    /// <summary>
    /// Termina una sessione specifica
    /// </summary>
    Task<Option<bool>> TerminateSessionAsync(Guid sessionId);
    
    /// <summary>
    /// Termina tutte le altre sessioni dell'utente corrente
    /// </summary>
    Task<Option<bool>> TerminateAllOtherSessionsAsync();
    
    /// <summary>
    /// Ottiene le statistiche delle sessioni per l'utente corrente
    /// </summary>
    Task<Option<SessionStatisticsDto>> GetMySessionStatisticsAsync();
    
    /// <summary>
    /// Ottiene la configurazione delle sessioni
    /// </summary>
    Task<Option<SessionConfigurationDto>> GetSessionConfigurationAsync();
    
    /// <summary>
    /// Aggiorna la configurazione delle sessioni
    /// </summary>
    Task<Option<SessionConfigurationDto>> UpdateSessionConfigurationAsync(SessionConfigurationDto config);
}