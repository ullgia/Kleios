namespace Kleios.Frontend.Shared;

/// <summary>
/// Rappresenta la registrazione di un modulo frontend presso il Gateway.
/// </summary>
public class ServiceRegistration
{
    /// <summary>
    /// Nome univoco del servizio (es: "auth-module", "home-module")
    /// </summary>
    public required string ServiceName { get; set; }

    /// <summary>
    /// Prefix per il routing (es: "/auth", "/system", "/")
    /// </summary>
    public required string RoutePrefix { get; set; }

    /// <summary>
    /// URL base del servizio (es: "https://localhost:5001")
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Endpoint per health check (es: "/_health")
    /// </summary>
    public required string HealthCheckEndpoint { get; set; }

    /// <summary>
    /// Timestamp di registrazione
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indica se il servizio è attualmente healthy
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Numero di failed health checks consecutivi
    /// </summary>
    public int FailedHealthChecks { get; set; } = 0;
}
