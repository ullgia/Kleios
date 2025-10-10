using Kleios.Frontend.Shared;

namespace Kleios.Gateway.Services;

/// <summary>
/// Service registry per gestire la registrazione e lookup dei moduli
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// Registra un nuovo servizio o aggiorna uno esistente
    /// </summary>
    Task RegisterServiceAsync(ServiceRegistration service);

    /// <summary>
    /// Trova un servizio in base al prefix della route
    /// </summary>
    /// <param name="routePrefix">Route prefix (es: /auth, /system, /)</param>
    /// <returns>ServiceRegistration se trovato, null altrimenti</returns>
    Task<ServiceRegistration?> GetServiceByPrefixAsync(string routePrefix);

    /// <summary>
    /// Ottiene tutti i servizi registrati
    /// </summary>
    Task<IEnumerable<ServiceRegistration>> GetAllServicesAsync();

    /// <summary>
    /// Deregistra un servizio
    /// </summary>
    Task UnregisterServiceAsync(string serviceName);

    /// <summary>
    /// Aggiorna lo stato di salute di un servizio
    /// </summary>
    Task UpdateHealthStatusAsync(string serviceName, bool isHealthy);

    /// <summary>
    /// Ottiene il numero di servizi registrati
    /// </summary>
    Task<int> GetServiceCountAsync();
}
