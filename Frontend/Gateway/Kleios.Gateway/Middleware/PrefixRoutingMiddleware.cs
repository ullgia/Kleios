using Kleios.Gateway.Services;
using Yarp.ReverseProxy.Forwarder;

namespace Kleios.Gateway.Middleware;

/// <summary>
/// Middleware per il routing basato su prefix matching
/// Intercetta le richieste e le instrada ai moduli appropriati tramite YARP
/// </summary>
public class PrefixRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PrefixRoutingMiddleware> _logger;

    public PrefixRoutingMiddleware(
        RequestDelegate next,
        ILogger<PrefixRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IServiceRegistry serviceRegistry,
        IHttpForwarder httpForwarder)
    {
        var path = context.Request.Path.Value ?? "/";

        // Ignora le richieste alle API del Gateway stesso
        if (path.StartsWith("/api/_gateway", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Ignora le richieste ai WebSocket del Gateway
        if (path.StartsWith("/ws/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Ignora richieste agli static assets
        if (path.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Cerca il servizio in base al prefix
        var service = await serviceRegistry.GetServiceByPrefixAsync(path);

        if (service == null)
        {
            _logger.LogWarning("Nessun servizio trovato per path: {Path}", path);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Route not found",
                path = path,
                message = "No service registered for this route prefix"
            });
            return;
        }

        // Log del routing
        _logger.LogDebug(
            "Routing {Path} → {ServiceName} ({BaseUrl})",
            path, service.ServiceName, service.BaseUrl);

        try
        {
            // Forward la richiesta al servizio usando YARP
            // IMPORTANTE: Non modifichiamo il path - il modulo gestisce il suo prefix
            var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                UseCookies = false
            });

            var error = await httpForwarder.SendAsync(
                context,
                service.BaseUrl,
                httpClient,
                ForwarderRequestConfig.Empty);

            if (error != ForwarderError.None)
            {
                var errorFeature = context.GetForwarderErrorFeature();
                var exception = errorFeature?.Exception;

                _logger.LogError(
                    exception,
                    "Errore nel forwarding verso {ServiceName}: {Error}",
                    service.ServiceName, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Eccezione durante il forwarding verso {ServiceName}",
                service.ServiceName);

            context.Response.StatusCode = 502; // Bad Gateway
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Service unavailable",
                service = service.ServiceName,
                message = "The upstream service is not responding"
            });
        }
    }
}

/// <summary>
/// Extension methods per registrare il middleware
/// </summary>
public static class PrefixRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UsePrefixRouting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PrefixRoutingMiddleware>();
    }
}
