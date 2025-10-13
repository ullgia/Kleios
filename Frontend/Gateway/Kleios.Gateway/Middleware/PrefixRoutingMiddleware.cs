using Kleios.Gateway.Services;
using Kleios.Frontend.Shared;
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

        // ⚠️ CRITICO: Forward richieste Blazor SignalR (/_blazor) al modulo corretto
        // Blazor Interactive Server usa SignalR per la comunicazione real-time
        if (path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase))
        {
            // Usa il Referer per determinare il modulo di origine
            var referer = context.Request.Headers.Referer.ToString();
            ServiceRegistration? targetService = null;

            if (!string.IsNullOrEmpty(referer))
            {
                var refererUri = new Uri(referer);
                var refererPath = refererUri.AbsolutePath;
                targetService = await serviceRegistry.GetServiceByPrefixAsync(refererPath);
            }

            // Fallback: modulo Home per SignalR senza referer
            if (targetService == null)
            {
                targetService = await serviceRegistry.GetServiceByPrefixAsync("/");
            }

            if (targetService != null)
            {
                _logger.LogInformation(
                    "Forwarding Blazor SignalR {Path} → {ServiceName} ({BaseUrl}) - Referer: {Referer}",
                    path, targetService.ServiceName, targetService.BaseUrl, referer);

                // Forward con supporto WebSocket
                var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
                {
                    UseProxy = false,
                    AllowAutoRedirect = false,
                    AutomaticDecompression = System.Net.DecompressionMethods.None,
                    UseCookies = true // ⚠️ IMPORTANTE: Mantieni cookies per autenticazione SignalR
                });

                var error = await httpForwarder.SendAsync(
                    context,
                    targetService.BaseUrl,
                    httpClient,
                    ForwarderRequestConfig.Empty);

                if (error != ForwarderError.None)
                {
                    var errorFeature = context.GetForwarderErrorFeature();
                    var exception = errorFeature?.Exception;

                    _logger.LogError(
                        exception,
                        "Errore nel forwarding Blazor SignalR {Path} verso {ServiceName}: {Error}",
                        path, targetService.ServiceName, error);
                }

                return;
            }
            else
            {
                _logger.LogWarning("Nessun servizio trovato per Blazor SignalR: {Path}", path);
                context.Response.StatusCode = 502;
                return;
            }
        }

        // Per gli static assets, usa il Referer header per determinare il modulo di destinazione
        // ECCEZIONE: shared.css è servito dal Gateway stesso (in wwwroot)
        var isStaticAsset = path.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase);

        // Se è shared.css, lascia che UseStaticFiles() lo gestisca
        if (path.Equals("/shared.css", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (isStaticAsset)
        {
            // Usa il Referer header per determinare il modulo di origine
            var referer = context.Request.Headers.Referer.ToString();
            ServiceRegistration? targetService = null;

            if (!string.IsNullOrEmpty(referer))
            {
                var refererUri = new Uri(referer);
                var refererPath = refererUri.AbsolutePath;
                targetService = await serviceRegistry.GetServiceByPrefixAsync(refererPath);
                
                _logger.LogDebug(
                    "Static asset {Path} - Referer: {Referer} → Path: {RefererPath} → Service: {ServiceName}",
                    path, referer, refererPath, targetService?.ServiceName ?? "null");
            }

            // Fallback: cerca il modulo root (Home) per asset senza referer o referer non valido
            if (targetService == null)
            {
                targetService = await serviceRegistry.GetServiceByPrefixAsync("/");
                _logger.LogDebug(
                    "Static asset {Path} - Nessun referer o servizio non trovato, usando Home module",
                    path);
            }

            if (targetService != null)
            {
                _logger.LogInformation(
                    "Forwarding static asset {Path} → {ServiceName} ({BaseUrl})",
                    path, targetService.ServiceName, targetService.BaseUrl);

                // Forward direttamente al modulo
                var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
                {
                    UseProxy = false,
                    AllowAutoRedirect = false,
                    AutomaticDecompression = System.Net.DecompressionMethods.None,
                    UseCookies = false
                });

                var error = await httpForwarder.SendAsync(
                    context,
                    targetService.BaseUrl,
                    httpClient,
                    ForwarderRequestConfig.Empty);

                if (error != ForwarderError.None)
                {
                    var errorFeature = context.GetForwarderErrorFeature();
                    var exception = errorFeature?.Exception;

                    _logger.LogError(
                        exception,
                        "Errore nel forwarding asset {Path} verso {ServiceName}: {Error}",
                        path, targetService.ServiceName, error);
                }
                
                return;
            }
            else
            {
                _logger.LogWarning("Nessun servizio trovato per static asset: {Path}", path);
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Asset not found",
                    path = path,
                    message = "No service available to serve this static asset"
                });
                return;
            }
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
