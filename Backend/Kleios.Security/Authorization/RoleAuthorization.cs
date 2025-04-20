using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Kleios.Security.Authorization;

/// <summary>
/// Handler di autorizzazione per verificare i ruoli utente
/// </summary>
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.CompletedTask;
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (requirement.AllowedRoles.Contains(userRole))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// Requisito di autorizzazione basato sui ruoli
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public RoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles ?? Array.Empty<string>();
    }
}

/// <summary>
/// Handler di autorizzazione per verificare i permessi utente
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Verifica se l'utente è un utente master con tutti i permessi
        if (context.User.HasClaim(c => c.Type == "IsMasterUser" && c.Value == "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Verifica permessi specifici
        if (!context.User.HasClaim(c => c.Type == "Permission"))
        {
            return Task.CompletedTask;
        }

        // Controlla se l'utente ha il permesso richiesto
        var userPermissions = context.User.FindAll(c => c.Type == "Permission").Select(c => c.Value);
        
        if (userPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// Requisito di autorizzazione basato sui permessi
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// Policy Names per l'autorizzazione
/// </summary>
public static class KleiosPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string UserOrAdmin = "UserOrAdmin";
    
    // Aggiungi altre policy secondo necessità
}

/// <summary>
/// Permessi specifici per l'autorizzazione
/// </summary>
public static class KleiosPermissions
{
    // Permessi per i logs
    public const string ViewLogs = "Logs.View";
    public const string ManageLogs = "Logs.Manage";
    
    // Permessi per gli utenti
    public const string ViewUsers = "Users.View";
    public const string ManageUsers = "Users.Manage";
    
    // Permessi per le impostazioni
    public const string ViewSettings = "Settings.View";
    public const string ManageSettings = "Settings.Manage";
    
    // Altri permessi possono essere aggiunti secondo necessità
}

/// <summary>
/// Helper per verificare i diritti di accesso dell'utente corrente
/// </summary>
public class UserAccessControl
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserAccessControl(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Verifica se l'utente corrente ha il ruolo specificato
    /// </summary>
    public bool HasRole(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated??false))
        {
            return false;
        }

        return user.HasClaim(ClaimTypes.Role, role);
    }
    
    /// <summary>
    /// Verifica se l'utente corrente ha il permesso specificato
    /// </summary>
    public bool HasPermission(string permission)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated??false))
        {
            return false;
        }
        
        // L'utente master ha tutti i permessi
        if (user.HasClaim(c => c.Type == "IsMasterUser" && c.Value == "true"))
        {
            return true;
        }

        return user.HasClaim("Permission", permission);
    }

    /// <summary>
    /// Verifica se l'utente corrente è master
    /// </summary>
    public bool IsMasterUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated??false))
        {
            return false;
        }

        return user.HasClaim(c => c.Type == "IsMasterUser" && c.Value == "true");
    }

    /// <summary>
    /// Verifica se l'utente corrente è admin
    /// </summary>
    public bool IsAdmin() => HasRole("Admin");

    /// <summary>
    /// Ottiene l'ID dell'utente corrente
    /// </summary>
    public Guid? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated??false))
        {
            return null;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId != null ? Guid.Parse(userId) : null;
    }
}