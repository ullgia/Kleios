// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\DependencyInjection.cs
using Kleios.Frontend.Infrastructure.Services;
using Kleios.Frontend.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kleios.Frontend.Infrastructure;

/// <summary>
/// Estensioni per la registrazione dei servizi Infrastructure
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra tutti i servizi di infrastructure
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Registra UserInfoState come servizio scoped per memorizzare le informazioni dell'utente
        // durante tutta la richiesta (funziona anche durante prerendering server)
        services.AddScoped<IdentityRedirectManager>();
        
        // Registra AuthenticatedHttpMessageHandler come transient
        // Nota: ora dipende solo da IAuthService e non direttamente da TokenManager
        services.AddTransient<AuthenticatedHttpMessageHandler>();

        // Configura HttpClient standard per le chiamate che non necessitano di autenticazione
        services.AddHttpClient();

        // Configura HttpClient per AuthService con service discovery di Aspire
        // Non usiamo l'handler di autenticazione per evitare dipendenze circolari
        services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS 
            // Aspire sostituirà l'URL con l'endpoint corretto del servizio
            client.BaseAddress = new Uri("https+http://auth-service");
        });

        // Configura HttpClient per LogsSettingsService con service discovery di Aspire e autenticazione
        ConfigureAuthenticatedHttpClient<ILogsSettingsService, LogsSettingsService>(services, "https+http://logs-settings-service");

        // Configura HttpClient per SystemAdministrationService (ex UserStateManager) con service discovery di Aspire e autenticazione
        ConfigureAuthenticatedHttpClient<ISystemAdministrationService, SystemAdministrationService>(services, "https+http://system-service");
        
        // Registra il servizio di menu
        services.AddScoped<IMenuService, MenuService>();

        return services;
    }
    
    /// <summary>
    /// Configura un HttpClient con autenticazione JWT automatica per un servizio specifico
    /// </summary>
    private static IHttpClientBuilder ConfigureAuthenticatedHttpClient<TInterface, TImplementation>(
        IServiceCollection services, 
        string baseAddress)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
        }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();
    }
}