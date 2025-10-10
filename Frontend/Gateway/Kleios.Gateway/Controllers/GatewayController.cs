using Kleios.Frontend.Shared;
using Kleios.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Gateway.Controllers;

/// <summary>
/// Controller per gestire la registrazione e il lookup dei servizi
/// </summary>
[ApiController]
[Route("api/_gateway")]
public class GatewayController : ControllerBase
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(
        IServiceRegistry serviceRegistry,
        ILogger<GatewayController> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuovo servizio nel Gateway
    /// POST /api/_gateway/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterService([FromBody] ServiceRegistration service)
    {
        if (service == null)
        {
            return BadRequest(new { error = "Service registration data is required" });
        }

        if (string.IsNullOrWhiteSpace(service.ServiceName))
        {
            return BadRequest(new { error = "ServiceName is required" });
        }

        if (string.IsNullOrWhiteSpace(service.RoutePrefix))
        {
            return BadRequest(new { error = "RoutePrefix is required" });
        }

        if (string.IsNullOrWhiteSpace(service.BaseUrl))
        {
            return BadRequest(new { error = "BaseUrl is required" });
        }

        try
        {
            await _serviceRegistry.RegisterServiceAsync(service);
            
            _logger.LogInformation(
                "Servizio {ServiceName} registrato con successo. Prefix: {RoutePrefix}, BaseUrl: {BaseUrl}",
                service.ServiceName, service.RoutePrefix, service.BaseUrl);

            return Ok(new
            {
                message = "Service registered successfully",
                serviceName = service.ServiceName,
                routePrefix = service.RoutePrefix,
                registeredAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la registrazione del servizio {ServiceName}", service.ServiceName);
            return StatusCode(500, new { error = "Internal server error during registration" });
        }
    }

    /// <summary>
    /// Ottiene la lista di tutte le route disponibili
    /// GET /api/_gateway/routes
    /// </summary>
    [HttpGet("routes")]
    public async Task<IActionResult> GetRoutes()
    {
        try
        {
            var services = await _serviceRegistry.GetAllServicesAsync();
            
            var routes = services
                .Where(s => s.IsHealthy) // Solo servizi healthy
                .Select(s => new
                {
                    s.ServiceName,
                    s.RoutePrefix,
                    s.RegisteredAt
                })
                .ToList();

            return Ok(routes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle routes");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Ottiene lo stato di tutti i servizi (admin endpoint)
    /// GET /api/_gateway/services
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        try
        {
            var services = await _serviceRegistry.GetAllServicesAsync();
            
            var serviceStatus = services.Select(s => new
            {
                s.ServiceName,
                s.RoutePrefix,
                s.BaseUrl,
                s.IsHealthy,
                s.FailedHealthChecks,
                s.RegisteredAt,
                status = s.IsHealthy ? "healthy" : "unhealthy"
            })
            .ToList();

            return Ok(new
            {
                totalServices = serviceStatus.Count,
                healthyServices = serviceStatus.Count(s => s.IsHealthy),
                unhealthyServices = serviceStatus.Count(s => !s.IsHealthy),
                services = serviceStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei servizi");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deregistra un servizio
    /// DELETE /api/_gateway/register/{serviceName}
    /// </summary>
    [HttpDelete("register/{serviceName}")]
    public async Task<IActionResult> UnregisterService(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return BadRequest(new { error = "Service name is required" });
        }

        try
        {
            await _serviceRegistry.UnregisterServiceAsync(serviceName);
            
            _logger.LogInformation("Servizio {ServiceName} deregistrato manualmente", serviceName);

            return Ok(new
            {
                message = "Service unregistered successfully",
                serviceName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la deregistrazione del servizio {ServiceName}", serviceName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
