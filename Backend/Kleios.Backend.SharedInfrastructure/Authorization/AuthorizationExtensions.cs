using Kleios.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kleios.Backend.SharedInfrastructure.Authorization;

/// <summary>
/// Extension methods per configurare l'authorization in modo centralizzato
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Configura tutte le Authorization Policies basate sui permessi definiti in AppPermissions.
    /// Questo metodo usa reflection per registrare automaticamente tutte le costanti
    /// definite nelle classi interne di AppPermissions come policy.
    /// </summary>
    public static IServiceCollection AddKleiosAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            AddAllPermissionsAsPolicies(options);
        });
        
        // Registra il Permission Handler per validare i permessi nei claims
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        
        return services;
    }
    
    /// <summary>
    /// Utilizza la reflection per registrare automaticamente tutte le costanti string 
    /// definite in AppPermissions come policy authorization
    /// </summary>
    private static void AddAllPermissionsAsPolicies(AuthorizationOptions options)
    {
        // Ottiene tutte le classi annidate in AppPermissions (es: Users, Roles, Settings)
        var nestedTypes = typeof(AppPermissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
        
        foreach (var nestedType in nestedTypes)
        {
            // Ottiene tutti i campi costanti di tipo string nella classe
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));
            
            foreach (var field in fields)
            {
                // Ottiene il valore del campo (il nome della permission es: "Users.View")
                string permission = (string)field.GetValue(null)!;
                
                // Registra la policy con requirement basato su claim
                options.AddPolicy(permission, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        }
    }
}

/// <summary>
/// Requirement per la validazione dei permessi.
/// Specifica quale permesso deve essere presente nei claims dell'utente.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Il nome del permesso richiesto (es: "Users.View")
    /// </summary>
    public string Permission { get; }
    
    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

/// <summary>
/// Handler che valida se l'utente ha il permesso richiesto nei suoi claims.
/// Viene invocato automaticamente dal framework quando un endpoint richiede un permesso.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>
    /// Valida se l'utente corrente ha il permesso richiesto
    /// </summary>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Cerca nei claims dell'utente un claim di tipo "permission" con valore uguale al permesso richiesto
        if (context.User.HasClaim(c => 
            c.Type == ApplicationClaimTypes.Permission && 
            c.Value == requirement.Permission))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}
