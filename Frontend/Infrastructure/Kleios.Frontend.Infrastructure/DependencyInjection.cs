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

        // Registra TokenManager
        services.AddScoped<TokenManager>();

        // Configura HttpClient per AuthService con service discovery di Aspire
        services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS 
            // Aspire sostituirà l'URL con l'endpoint corretto del servizio
            client.BaseAddress = new Uri("https+http://auth-service");
        });

        // Configura HttpClient per LogsSettingsService con service discovery di Aspire
        services.AddHttpClient<ILogsSettingsService, LogsSettingsService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS
            client.BaseAddress = new Uri("https+http://logs-settings-service");
        });

        // Configura HttpClient per UserManagementService con service discovery di Aspire
        services.AddHttpClient<IUserManagementService, UserManagementService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS
            client.BaseAddress = new Uri("https+http://user-management-service");
        });
        
        // Registra il servizio di menu
        services.AddScoped<IMenuService, MenuService>();

        // HttpClient standard per le chiamate che non necessitano di autenticazione
        services.AddHttpClient();

        return services;
    }
}