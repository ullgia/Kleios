using Kleios.Frontend.Shared;

namespace Kleios.Module.Auth.Services;

/// <summary>
/// BackgroundService che gestisce la registrazione del modulo Auth al Gateway
/// e mantiene attiva la connessione WebSocket per il heartbeat
/// </summary>
public class AuthModuleRegistration : BackgroundService
{
    private readonly ILogger<AuthModuleRegistration> _logger;
    private readonly IConfiguration _configuration;
    private readonly GatewayConnectionClient _gatewayClient;
    private readonly string _gatewayUrl;

    public AuthModuleRegistration(
        ILogger<AuthModuleRegistration> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<GatewayConnectionClient> gatewayLogger)
    {
        _logger = logger;
        _configuration = configuration;
        _gatewayUrl = _configuration["GatewayUrl"] ?? "https://localhost:5000";
        
        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_gatewayUrl);
        _gatewayClient = new GatewayConnectionClient(httpClient, gatewayLogger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Attendi 5 secondi prima di registrarsi per dare tempo al Gateway di avviarsi
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("Starting Auth Module registration with Gateway");

        var moduleUrl = _configuration["ModuleUrl"] ?? "https://localhost:5001";
        var registration = new ServiceRegistration
        {
            ServiceName = "auth-module",
            RoutePrefix = "/auth",
            BaseUrl = moduleUrl,
            HealthCheckEndpoint = "/_health",
            RegisteredAt = DateTime.UtcNow
        };

        try
        {
            // Registra il modulo al Gateway
            var registered = await _gatewayClient.RegisterAsync(registration, stoppingToken);
            
            if (registered)
            {
                _logger.LogInformation("Auth Module successfully registered with Gateway at {GatewayUrl}", 
                    _gatewayUrl);

                // Connetti WebSocket per heartbeat
                await _gatewayClient.ConnectWebSocketAsync(registration.ServiceName, _gatewayUrl, stoppingToken);
                
                _logger.LogInformation("WebSocket connection established for heartbeat");
            }
            else
            {
                _logger.LogError("Failed to register Auth Module with Gateway");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Auth Module registration");
        }

        // Mantieni il servizio in esecuzione
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Auth Module and disconnecting from Gateway");
        
        _gatewayClient.Dispose();
        
        await base.StopAsync(cancellationToken);
    }
}
