using FluentValidation;
using FluentValidation.AspNetCore;
using Kleios.Backend.SharedInfrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kleios.Backend.SharedInfrastructure.Validation;

/// <summary>
/// Estensioni per la configurazione di FluentValidation
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Aggiunge FluentValidation al servizio di MVC per la validazione automatica delle richieste
    /// </summary>
    /// <param name="services">La collezione di servizi</param>
    /// <param name="assemblies">Gli assembly contenenti i validatori</param>
    /// <returns>IServiceCollection per chaining</returns>
    public static IServiceCollection AddKleiosValidation(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Registra il filtro di validazione
        services.AddScoped<ValidationFilter>();
        
        // Configura i controller con il filtro di validazione
        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });
        
        // Disabilita la validazione automatica dei ModelState in modo che venga gestita dal nostro filtro
        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });
        
        // Aggiunge sempre l'assembly di Kleios.Shared che contiene i validatori principali
        var assembliesList = assemblies.ToList();
        
        // Aggiungiamo l'assembly di Kleios.Shared solo se esiste
        try
        {
            var sharedAssembly = Assembly.Load("Kleios.Shared");
            if (!assembliesList.Contains(sharedAssembly))
            {
                assembliesList.Add(sharedAssembly);
            }
        }
        catch (Exception)
        {
            // L'assembly Kleios.Shared non è stato trovato, ma possiamo continuare
        }
        
        // Registra i validatori dagli assembly specificati
        foreach (var assembly in assembliesList)
        {
            services.AddValidatorsFromAssembly(assembly);
        }
        
        return services;
    }
}