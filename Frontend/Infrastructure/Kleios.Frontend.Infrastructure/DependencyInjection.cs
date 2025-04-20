// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\DependencyInjection.cs
using Kleios.Frontend.Infrastructure.Handlers;
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
        services.AddScoped<UserInfoState>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<ILocalStorageService, LocalStorageService>();
        services.AddScoped<ITokenService, TokenService>();

        // Registra l'interceptor HTTP
        services.AddScoped<AuthHttpInterceptor>();

        // Configura HttpClient per AuthService con service discovery di Aspire
        services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS 
            // Aspire sostituirà l'URL con l'endpoint corretto del servizio
            client.BaseAddress = new Uri("https+http://auth-service");
        }).AddHttpMessageHandler<AuthHttpInterceptor>();

        // Configura HttpClient per LogsSettingsService con service discovery di Aspire
        services.AddHttpClient<ILogsSettingsService, LogsSettingsService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS
            client.BaseAddress = new Uri("https+http://logs-settings-service");
        }).AddHttpMessageHandler<AuthHttpInterceptor>();

        // Configura HttpClient per UserManagementService con service discovery di Aspire
        services.AddHttpClient<IUserManagementService, UserManagementService>(client =>
        {
            // Usa service discovery schema con preferenza HTTPS
            client.BaseAddress = new Uri("https+http://user-management-service");
        }).AddHttpMessageHandler<AuthHttpInterceptor>();

        // Configura HttpClient generico per altri servizi che possono ancora utilizzare IHttpService
        services.AddHttpClient("API")
            .AddHttpMessageHandler<AuthHttpInterceptor>();

        // HttpClient standard per le chiamate che non necessitano di autenticazione
        services.AddHttpClient();

        return services;
    }
}