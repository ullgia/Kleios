using Kleios.Backend.Shared;
using Kleios.Backend.SharedInfrastructure.Authentication;
using Kleios.Backend.SharedInfrastructure.Middleware;
using Kleios.Backend.SharedInfrastructure.Services;
using Kleios.Backend.SharedInfrastructure.Validation;
using Kleios.Database.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public static IApplicationBuilder UseKleiosInfrastructure(this IApplicationBuilder app)
    {
        // Gestione centralizzata degli errori
        app.UseErrorHandling();
        
        // Configurazione dell'autenticazione
        app.UseAuthentication();
        app.UseAuthorization();
        
        return app;
    }
}