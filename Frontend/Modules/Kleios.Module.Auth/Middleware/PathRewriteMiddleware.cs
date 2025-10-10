namespace Kleios.Module.Auth.Middleware;

/// <summary>
/// Middleware che rimuove il prefisso /auth dai path delle richieste
/// in modo che il modulo possa gestirle correttamente
/// </summary>
public class PathRewriteMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _prefix;

    public PathRewriteMiddleware(RequestDelegate next, string prefix)
    {
        _next = next;
        _prefix = prefix.StartsWith('/') ? prefix : $"/{prefix}";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Se il path inizia con il prefisso, rimuovilo
        if (path.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path.Substring(_prefix.Length);
            
            // Se il remainder è vuoto, imposta "/"
            if (string.IsNullOrEmpty(remainder) || remainder == "/")
            {
                remainder = "/";
            }
            
            // IMPORTANTE: Imposta PathBase per Blazor routing
            context.Request.PathBase = _prefix;
            context.Request.Path = remainder;
            
            // Log per debugging
            var logger = context.RequestServices.GetRequiredService<ILogger<PathRewriteMiddleware>>();
            logger.LogDebug(
                "Path rewrite: {OriginalPath} → PathBase: {PathBase}, Path: {NewPath}",
                path, context.Request.PathBase, context.Request.Path);
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods per registrare il PathRewriteMiddleware
/// </summary>
public static class PathRewriteMiddlewareExtensions
{
    public static IApplicationBuilder UsePathRewrite(this IApplicationBuilder builder, string prefix)
    {
        return builder.UseMiddleware<PathRewriteMiddleware>(prefix);
    }
}
