using Kleios.Backend.Shared;
using Kleios.Backend.SharedInfrastructure.Authentication;
using Kleios.Backend.SharedInfrastructure.Middleware;
using Kleios.Backend.SharedInfrastructure.Services;
using Kleios.Backend.SharedInfrastructure.Validation;
using Kleios.Database.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;

namespace Kleios.Backend.SharedInfrastructure;

/// <summary>
/// Estensioni per configurare l'infrastruttura condivisa tra i progetti backend
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Aggiunge i servizi dell'infrastruttura condivisa
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        // Registra il servizio SettingsService
        services.AddScoped<ISettingsService, SettingsService>();
        
        // Registra il servizio EmailService
        services.AddScoped<IEmailService, EmailService>();
        
        return services;
    }

    /// <summary>
    /// Aggiunge i servizi dell'infrastruttura condivisa e configura l'autenticazione Kleios
    /// </summary>
    public static IServiceCollection AddKleiosInfrastructure<TContext>(this IServiceCollection services, params Assembly[] validationAssemblies)
        where TContext : DbContext
    {
        // Aggiunge i servizi base dell'infrastruttura
        services.AddSharedInfrastructure();
        
        // Configura l'autenticazione Kleios
        services.AddKleiosAuthentication<TContext>();
        
        // Configura la validazione automatica delle richieste
        services.AddKleiosValidation(validationAssemblies);
        
        return services;
    }
    
    /// <summary>
    /// Aggiunge Health Checks per i microservizi Kleios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration per connection string</param>
    /// <returns>IServiceCollection per chaining</returns>
    public static IServiceCollection AddKleiosHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Nota: il check "self" è già aggiunto da AddDefaultHealthChecks() nei ServiceDefaults di Aspire
        var healthChecksBuilder = services.AddHealthChecks();

        // Aggiungi SQL Server health check se connection string presente
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddSqlServer(connectionString, name: "database", tags: new[] { "ready" });
        }

        return services;
    }
    
    /// <summary>
    /// Aggiunge il middleware per la gestione degli errori
    /// </summary>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<ErrorHandlingMiddleware>();
        return app;
    }

    /// <summary>
    /// Configura l'applicazione Kleios con i middleware necessari
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="configuration">Configuration per CORS policy name</param>
    /// <returns>IApplicationBuilder per chaining</returns>
    public static IApplicationBuilder UseKleiosInfrastructure(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        // Security Headers (primo nella pipeline)
        app.UseSecurityHeaders();
        
        // CORS (prima di authentication)
        var corsPolicy = configuration.GetSection("Cors")["PolicyName"] ?? "KleiosPolicy";
        app.UseCors(corsPolicy);
        
        // Gestione centralizzata degli errori
        app.UseErrorHandling();
        
        // Audit Logging (dopo error handling, prima di routing)
        app.UseAuditLogging();
        
        // Routing (necessario prima di authentication/authorization)
        app.UseRouting();
        
        // Configurazione dell'autenticazione
        app.UseAuthentication();
        app.UseAuthorization();
        
        return app;
    }

    /// <summary>
    /// Mappa gli endpoint Health Checks per i microservizi Kleios
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>IApplicationBuilder per chaining</returns>
    public static IApplicationBuilder MapKleiosHealthChecks(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            // Health check principale
            endpoints.MapHealthChecks("/health");

            // Ready check (con database)
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });

            // Live check (solo self)
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });
        });

        return app;
    }
}