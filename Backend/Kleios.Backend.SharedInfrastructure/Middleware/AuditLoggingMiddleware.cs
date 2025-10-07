using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kleios.Backend.SharedInfrastructure.Middleware;

/// <summary>
/// Middleware per l'audit logging delle richieste sensibili
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // Percorsi che richiedono audit logging
    private static readonly HashSet<string> _auditPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/refresh",
        "/api/system/users",
        "/api/system/roles",
        "/api/system/settings"
    };

    // Metodi HTTP che modificano dati
    private static readonly HashSet<string> _auditMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "DELETE", "PATCH"
    };

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Verifica se la richiesta richiede audit logging
        var requiresAudit = ShouldAudit(context);
        
        if (!requiresAudit)
        {
            await _next(context);
            return;
        }

        // Cattura informazioni prima della richiesta
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId(context);
        var ipAddress = GetIpAddress(context);
        var requestPath = context.Request.Path.Value ?? "unknown";
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();

        // Cattura il body della richiesta (solo per operazioni sensibili)
        string? requestBody = null;
        if (context.Request.ContentLength > 0 && context.Request.ContentLength < 10000)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Cattura la risposta
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log dell'audit
            var statusCode = context.Response.StatusCode;
            var duration = stopwatch.ElapsedMilliseconds;

            LogAuditEvent(
                userId,
                ipAddress,
                requestPath,
                requestMethod,
                statusCode,
                duration,
                userAgent,
                requestBody);

            // Ripristina il body della risposta
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Determina se la richiesta richiede audit logging
    /// </summary>
    private bool ShouldAudit(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method;

        // Audit per percorsi specifici
        if (_auditPaths.Any(ap => path.StartsWith(ap, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Audit per metodi che modificano dati su API
        if (_auditMethods.Contains(method) && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ottiene l'ID utente dal contesto
    /// </summary>
    private string GetUserId(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                return userIdClaim.Value;
            }
        }
        return "Anonymous";
    }

    /// <summary>
    /// Ottiene l'indirizzo IP del client
    /// </summary>
    private string GetIpAddress(HttpContext context)
    {
        // Verifica header X-Forwarded-For per proxy/load balancer
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Altrimenti usa l'IP remoto
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Logga l'evento di audit
    /// </summary>
    private void LogAuditEvent(
        string userId,
        string ipAddress,
        string requestPath,
        string requestMethod,
        int statusCode,
        long duration,
        string userAgent,
        string? requestBody)
    {
        var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        
        _logger.Log(
            logLevel,
            "[AUDIT] User:{UserId} IP:{IpAddress} {Method} {Path} Status:{StatusCode} Duration:{Duration}ms UA:{UserAgent} Body:{HasBody}",
            userId,
            ipAddress,
            requestMethod,
            requestPath,
            statusCode,
            duration,
            userAgent,
            string.IsNullOrEmpty(requestBody) ? "No" : "Yes");

        // Log separato per il body (solo in caso di errore o per login)
        if (requestBody != null && (statusCode >= 400 || requestPath.Contains("/login", StringComparison.OrdinalIgnoreCase)))
        {
            // Sanitizza il body per non loggare password in chiaro
            var sanitizedBody = SanitizeRequestBody(requestBody, requestPath);
            
            _logger.LogInformation(
                "[AUDIT-BODY] User:{UserId} Path:{Path} Body:{Body}",
                userId,
                requestPath,
                sanitizedBody);
        }
    }

    /// <summary>
    /// Rimuove dati sensibili dal body della richiesta
    /// </summary>
    private string SanitizeRequestBody(string body, string path)
    {
        // Per richieste di login, nascondi la password
        if (path.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            // Sostituisce il valore della password con ***
            return System.Text.RegularExpressions.Regex.Replace(
                body,
                @"""password""\s*:\s*""[^""]*""",
                @"""password"":""***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return body.Length > 500 ? body.Substring(0, 500) + "..." : body;
    }
}

/// <summary>
/// Estensioni per registrare il middleware di audit logging
/// </summary>
public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}
