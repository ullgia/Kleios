using System.Text;
using Kleios.Backend.Shared;
using Kleios.Database.Context;
using Kleios.Shared.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Kleios.Backend.SharedInfrastructure.Authentication;

/// <summary>
/// Estensioni per la configurazione dell'autenticazione in Kleios
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Aggiunge il sistema di autenticazione JWT per Kleios
    /// </summary>
    /// <typeparam name="TContext">Tipo del DbContext</typeparam>
    /// <param name="services">Collection di servizi</param>
    /// <returns>IServiceCollection per chaining</returns>
    public static IServiceCollection AddKleiosAuthentication<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        // Configura l'autenticazione con un handler JWT personalizzato che leggerà le impostazioni on-demand
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            
            // Usiamo un evento per configurare i parametri di validazione on-demand
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    // Qui possiamo eseguire validazioni aggiuntive sul token
                },
                
                OnMessageReceived = async context =>
                {
                    // Ottenere un IServiceScope per risolvere i servizi
                    using var scope = context.HttpContext.RequestServices.CreateScope();
                    var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
                    
                    // Legge le impostazioni JWT in modo asincrono
                    var jwtSecretOption = await settingsService.GetSettingByKeyAsync("Jwt:SecretKey");
                    var jwtIssuerOption = await settingsService.GetSettingByKeyAsync("Jwt:Issuer");
                    var jwtAudienceOption = await settingsService.GetSettingByKeyAsync("Jwt:Audience");
                    
                    if (!jwtSecretOption.IsSuccess || !jwtIssuerOption.IsSuccess || !jwtAudienceOption.IsSuccess)
                    {
                        context.Fail("Impossibile caricare le impostazioni JWT necessarie per l'autenticazione");
                        return;
                    }
                    
                    var jwtSecret = jwtSecretOption.Value.Value;
                    var jwtIssuer = jwtIssuerOption.Value.Value;
                    var jwtAudience = jwtAudienceOption.Value.Value;
                    
                    if (string.IsNullOrEmpty(jwtSecret))
                    {
                        context.Fail("La chiave segreta JWT non può essere vuota");
                        return;
                    }
                    
                    // Configura i parametri di validazione del token
                    context.Options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ClockSkew = TimeSpan.Zero
                    };
                }
            };
        });
        
        // Configura l'autorizzazione
        services.AddAuthorization();
        
        return services;
    }
}