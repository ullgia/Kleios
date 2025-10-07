using System.Net;
using System.Net.Http.Headers;
using Kleios.Frontend.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Handler HTTP personalizzato che gestisce automaticamente l'autenticazione con JWT
/// Verifica la validità del token, esegue il refresh se necessario e gestisce gli errori
/// </summary>
public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly ITokenDistributionService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticatedHttpMessageHandler> _logger;

    public AuthenticatedHttpMessageHandler(
        ITokenDistributionService tokenService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticatedHttpMessageHandler> logger)
    {
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Ignora l'autenticazione per le richieste di login, registrazione e refresh token
        if (IsAuthEndpoint(request.RequestUri))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Aggiunge il token di autenticazione alla richiesta
        await AddAuthenticationHeader(request, false, cancellationToken);

        // Invia la richiesta
        var response = await base.SendAsync(request, cancellationToken);

        // Gestisce risposta 401 (Unauthorized) tentando un refresh del token e riprovando la richiesta
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Risposta 401 ricevuta, tentativo di refresh e retry");
            
            // Invalida eventuali cache, forzando un refresh del token
            await AddAuthenticationHeader(request, true, cancellationToken);
            
            // Ripeti la richiesta
            return await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Verifica se l'endpoint è relativo all'autenticazione (per cui non serve autenticazione)
    /// </summary>
    private bool IsAuthEndpoint(Uri? requestUri)
    {
        if (requestUri == null) return false;
        
        var path = requestUri.PathAndQuery.ToLowerInvariant();
        return path.Contains("/api/auth/login") || 
               path.Contains("/api/auth/register") || 
               path.Contains("/api/auth/refresh");
    }

    /// <summary>
    /// Aggiunge l'header di autenticazione alla richiesta
    /// </summary>
    private async Task AddAuthenticationHeader(HttpRequestMessage request, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Ottieni l'userId dal contesto HTTP
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogDebug("Nessun utente autenticato nel contesto HTTP corrente, skip authentication header");
            return;
        }
        
        // Prendiamo l'ID di correlazione dalla richiesta HTTP se disponibile
        string? correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        
        // Ottieni un token valido usando il nuovo TokenDistributionService
        var tokenResult = await _tokenService.GetValidTokenAsync(userId, correlationId);
        
        if (tokenResult.IsSuccess)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);
            _logger.LogDebug("Token JWT aggiunto all'header della richiesta");
        }
        else
        {
            _logger.LogWarning("Impossibile ottenere un token valido: {Error}", tokenResult.Message);
        }
    }
    
    /// <summary>
    /// Ottiene l'ID utente corrente dal contesto HTTP
    /// Gestisce il caso in cui HttpContext sia null (es. background services)
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // HttpContext può essere null in background services o fuori da richieste HTTP
        if (httpContext == null)
        {
            _logger.LogTrace("HttpContext è null - probabile esecuzione in background service");
            return Guid.Empty;
        }
        
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
        }
        
        return Guid.Empty;
    }
}