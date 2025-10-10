using Kleios.Frontend.Shared;

namespace Kleios.Gateway.Services;

/// <summary>
/// Background service che monitora la salute dei servizi registrati
/// </summary>
public class ServiceHealthMonitor : BackgroundService
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceHealthMonitor> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(KleiosConstants.HealthCheck.IntervalSeconds);
    private readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(KleiosConstants.HealthCheck.TimeoutSeconds);

    public ServiceHealthMonitor(
        IServiceRegistry serviceRegistry,
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceHealthMonitor> logger)
    {
        _serviceRegistry = serviceRegistry;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service Health Monitor avviato. Intervallo check: {Interval}s", 
            KleiosConstants.HealthCheck.IntervalSeconds);

        // Aspetta un po' prima di iniziare i check (dare tempo ai servizi di avviarsi)
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllServicesHealthAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il check della salute dei servizi");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Service Health Monitor arrestato");
    }

    private async Task CheckAllServicesHealthAsync(CancellationToken cancellationToken)
    {
        var services = await _serviceRegistry.GetAllServicesAsync();
        var servicesList = services.ToList();

        if (!servicesList.Any())
        {
            return; // Nessun servizio da controllare
        }

        _logger.LogDebug("Check salute di {Count} servizi", servicesList.Count);

        foreach (var service in servicesList)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await CheckServiceHealthAsync(service, cancellationToken);
        }
    }

    private async Task CheckServiceHealthAsync(ServiceRegistration service, CancellationToken cancellationToken)
    {
        try
        {
            var healthUrl = $"{service.BaseUrl}{service.HealthCheckEndpoint}";
            
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = _httpTimeout;

            var response = await httpClient.GetAsync(healthUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await _serviceRegistry.UpdateHealthStatusAsync(service.ServiceName, isHealthy: true);
            }
            else
            {
                _logger.LogWarning(
                    "Health check failed per {ServiceName}: HTTP {StatusCode}",
                    service.ServiceName, response.StatusCode);
                
                await _serviceRegistry.UpdateHealthStatusAsync(service.ServiceName, isHealthy: false);
                await CheckForDeregistrationAsync(service);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "Health check timeout per {ServiceName} dopo {Timeout}s",
                service.ServiceName, _httpTimeout.TotalSeconds);
            
            await _serviceRegistry.UpdateHealthStatusAsync(service.ServiceName, isHealthy: false);
            await CheckForDeregistrationAsync(service);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Health check errore per {ServiceName}: {Message}",
                service.ServiceName, ex.Message);
            
            await _serviceRegistry.UpdateHealthStatusAsync(service.ServiceName, isHealthy: false);
            await CheckForDeregistrationAsync(service);
        }
    }

    private async Task CheckForDeregistrationAsync(ServiceRegistration service)
    {
        // Se il servizio ha superato il numero massimo di check falliti, deregistralo
        if (service.FailedHealthChecks >= KleiosConstants.HealthCheck.MaxFailedChecks)
        {
            _logger.LogError(
                "Servizio {ServiceName} ha superato {MaxFailedChecks} health check falliti. Deregistrazione in corso...",
                service.ServiceName, KleiosConstants.HealthCheck.MaxFailedChecks);
            
            await _serviceRegistry.UnregisterServiceAsync(service.ServiceName);
        }
    }
}
