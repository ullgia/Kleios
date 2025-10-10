using System.Collections.Concurrent;
using Kleios.Frontend.Shared;

namespace Kleios.Gateway.Services;

/// <summary>
/// Implementazione in-memory del service registry con ConcurrentDictionary per thread-safety
/// </summary>
public class InMemoryServiceRegistry : IServiceRegistry
{
    private readonly ConcurrentDictionary<string, ServiceRegistration> _services = new();
    private readonly ILogger<InMemoryServiceRegistry> _logger;

    public InMemoryServiceRegistry(ILogger<InMemoryServiceRegistry> logger)
    {
        _logger = logger;
    }

    public Task RegisterServiceAsync(ServiceRegistration service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        _services.AddOrUpdate(
            service.ServiceName,
            service,
            (key, existingService) =>
            {
                // Aggiorna il servizio esistente mantenendo lo stato di salute precedente se disponibile
                service.IsHealthy = existingService.IsHealthy;
                service.FailedHealthChecks = existingService.FailedHealthChecks;
                service.RegisteredAt = DateTime.UtcNow; // Aggiorna timestamp registrazione
                
                _logger.LogInformation(
                    "Servizio {ServiceName} re-registrato con prefix {RoutePrefix} su {BaseUrl}",
                    service.ServiceName, service.RoutePrefix, service.BaseUrl);
                
                return service;
            });

        _logger.LogInformation(
            "Servizio {ServiceName} registrato con prefix {RoutePrefix} su {BaseUrl}",
            service.ServiceName, service.RoutePrefix, service.BaseUrl);

        return Task.CompletedTask;
    }

    public Task<ServiceRegistration?> GetServiceByPrefixAsync(string routePrefix)
    {
        if (string.IsNullOrEmpty(routePrefix))
            return Task.FromResult<ServiceRegistration?>(null);

        // IMPORTANTE: Ordina per lunghezza del prefix (longest match first)
        // Questo garantisce che /system/Users matchi /system prima di /
        var matchingService = _services.Values
            .Where(s => s.IsHealthy && routePrefix.StartsWith(s.RoutePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.RoutePrefix.Length)
            .FirstOrDefault();

        return Task.FromResult(matchingService);
    }

    public Task<IEnumerable<ServiceRegistration>> GetAllServicesAsync()
    {
        // Ordina per prefix length (longest first) per visualizzazione consistente
        var services = _services.Values
            .OrderByDescending(s => s.RoutePrefix.Length)
            .AsEnumerable();

        return Task.FromResult(services);
    }

    public Task UnregisterServiceAsync(string serviceName)
    {
        if (_services.TryRemove(serviceName, out var removedService))
        {
            _logger.LogWarning(
                "Servizio {ServiceName} deregistrato (prefix: {RoutePrefix})",
                serviceName, removedService.RoutePrefix);
        }

        return Task.CompletedTask;
    }

    public Task UpdateHealthStatusAsync(string serviceName, bool isHealthy)
    {
        if (_services.TryGetValue(serviceName, out var service))
        {
            var previousHealth = service.IsHealthy;
            service.IsHealthy = isHealthy;

            if (isHealthy)
            {
                service.FailedHealthChecks = 0; // Reset failed checks counter
                if (!previousHealth)
                {
                    _logger.LogInformation(
                        "Servizio {ServiceName} è tornato healthy",
                        serviceName);
                }
            }
            else
            {
                service.FailedHealthChecks++;
                _logger.LogWarning(
                    "Servizio {ServiceName} è unhealthy (failed checks: {FailedChecks})",
                    serviceName, service.FailedHealthChecks);
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> GetServiceCountAsync()
    {
        return Task.FromResult(_services.Count);
    }
}
