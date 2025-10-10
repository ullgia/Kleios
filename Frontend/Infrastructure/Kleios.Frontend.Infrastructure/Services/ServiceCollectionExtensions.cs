using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Kleios.Frontend.Infrastructure.Authentication;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Extension methods per registrare i servizi Kleios Infrastructure
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Aggiunge tutti i servizi Infrastructure Kleios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="backendUrl">URL del backend per HttpClient</param>
    public static IServiceCollection AddKleiosInfrastructure(
        this IServiceCollection services,
        string backendUrl)
    {
        // HttpContextAccessor (necessario per cookie)
        services.AddHttpContextAccessor();

        // HttpClient per AuthenticationService
        services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            client.BaseAddress = new Uri(backendUrl);
        });

        // AuthenticationStateProvider
        services.AddScoped<AuthenticationStateProvider, ServerCookieAuthenticationStateProvider>();

        // TODO: Add Authorization policies if needed

        return services;
    }
}
