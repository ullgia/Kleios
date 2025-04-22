using Kleios.Database.Models;
using Kleios.Security.Authentication;
using Kleios.Security.Authorization;
using Kleios.Shared.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Reflection;
using Kleios.Database.Extensions;

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
        services.AddKleiosDatabase(useInMemoryDatabase: true);
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
            var nestedTypes = typeof(AppPermissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

            foreach (var nestedType in nestedTypes)
            {
                // Ottiene tutti i campi costanti di tipo string nella classe
                var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string));

                foreach (var field in fields)
                {
                    var propertyValue = field.GetValue(null);
                    if (propertyValue is not null)
                    {
                        options.AddPolicy(propertyValue.ToString()!, policy => policy
                            .RequireAuthenticatedUser()
                            .RequireClaim(ApplicationClaimTypes.Permission, propertyValue.ToString()!));
                    }
                }
            }
        });
        
        // Registra gli handler di autorizzazione
        services.AddSingleton<IAuthorizationHandler, RoleAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        
        return services;
    }

 
}