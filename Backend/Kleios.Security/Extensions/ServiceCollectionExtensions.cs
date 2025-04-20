using Kleios.Database.Models;
using Kleios.Security.Authentication;
using Kleios.Security.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Kleios.Security.Extensions;

/// <summary>
/// Estensioni per la configurazione dei servizi di sicurezza
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Aggiunge e configura i servizi di autenticazione e autorizzazione
    /// </summary>
    public static IServiceCollection AddKleiosSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configura Identity (si assume che AddKleiosDatabase sia stato chiamato in precedenza)
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Impostazioni delle policy delle password
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                
                // Altre impostazioni di Identity
                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<Kleios.Database.Context.KleiosDbContext>()
            .AddDefaultTokenProviders();
            
        // Registra il servizio di autenticazione
        services.AddScoped<IAuthService, AuthService>();
        
        // Aggiunge l'HTTP Context Accessor per UserAccessControl
        services.AddHttpContextAccessor();
        services.AddScoped<UserAccessControl>();
        
        // Configura l'autenticazione JWT
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["JWT:ValidIssuer"],
                ValidAudience = configuration["JWT:ValidAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"] ?? "Kleios_JWT_Secret_Key_For_Auth_At_Least_32_Characters"))
            };
        });
        
        // Configura l'autorizzazione
        services.AddAuthorization(options =>
        {
            // Aggiunge le policy basate su ruoli
            options.AddPolicy(KleiosPolicies.AdminOnly, policy => 
                policy.Requirements.Add(new RoleRequirement("Admin")));
                
            options.AddPolicy(KleiosPolicies.UserOrAdmin, policy => 
                policy.Requirements.Add(new RoleRequirement("Admin", "User")));
                
            // Aggiunge policy basate sui permessi
            // Per i logs
            options.AddPolicy(KleiosPermissions.ViewLogs, policy => 
                policy.Requirements.Add(new PermissionRequirement(KleiosPermissions.ViewLogs)));
                
            options.AddPolicy(KleiosPermissions.ManageLogs, policy => 
                policy.Requirements.Add(new PermissionRequirement(KleiosPermissions.ManageLogs)));
                
            // Per gli utenti
            options.AddPolicy(KleiosPermissions.ViewUsers, policy => 
                policy.Requirements.Add(new PermissionRequirement(KleiosPermissions.ViewUsers)));
                
            options.AddPolicy(KleiosPermissions.ManageUsers, policy => 
                policy.Requirements.Add(new PermissionRequirement(KleiosPermissions.ManageUsers)));
                
            // Per le impostazioni
            options.AddPolicy(KleiosPermissions.ViewSettings, policy => 
                policy.Requirements.Add(new PermissionRequirement(KleiosPermissions.ViewSettings)));
                
            options.AddPolicy(KleiosPermissions.ManageSettings, policy => 
                policy.Requirements.Add(new PermissionRequirement(KleiosPermissions.ManageSettings)));
        });
        
        // Registra gli handler di autorizzazione
        services.AddSingleton<IAuthorizationHandler, RoleAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        
        return services;
    }
}