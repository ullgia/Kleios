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
        
        // Registra il servizio per la gestione dello storage dei token
        services.AddScoped<ITokenStore, DistributedCacheTokenStore>();
        
        // Registra il servizio per la sicurezza dei token
        services.AddScoped<TokenSecurityService>();
        
        // Registra il servizio di refresh token per risolvere la dipendenza circolare
        services.AddHttpClient<ITokenRefreshService, TokenRefreshService>(client =>
        {
            client.BaseAddress = new Uri("https+http://auth-service");
        });
        
        // Registra il nuovo servizio TokenDistributionService per la gestione dei token
        // che funziona sia con server rendering che con altre modalità
        services.AddScoped<ITokenDistributionService, TokenDistributionService>();
        
        // Non registriamo TokenDistributionService come ICircuitHandler perché non è più necessario
        
        // Registra AuthenticatedHttpMessageHandler come transient
        // Ora dipenderà dal nuovo TokenDistributionService
        services.AddTransient<AuthenticatedHttpMessageHandler>();

        // Configura HttpClient standard per le chiamate che non necessitano di autenticazione
        services.AddHttpClient();

        // Configura HttpClient per AuthService con service discovery di Aspire
        // Non usiamo l'handler di autenticazione per evitare dipendenze circolari
        services.AddHttpClient<IFrontendAuthService, AuthService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS 
            // Aspire sostituirà l'URL con l'endpoint corretto del servizio
            client.BaseAddress = new Uri("https+http://auth-backend");
        });

        // Configura HttpClient per LogsSettingsService con service discovery di Aspire e autenticazione
        ConfigureAuthenticatedHttpClient<ILogsSettingsService, LogsSettingsService>(services, "https+http://system-backend");

        // Configura HttpClient per SystemAdministrationService (ex UserStateManager) con service discovery di Aspire e autenticazione
        ConfigureAuthenticatedHttpClient<ISystemAdministrationService, SystemAdministrationService>(services, "https+http://system-backend");
        
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