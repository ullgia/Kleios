using System.Net;
using System.Net.Http.Headers;
using Kleios.Frontend.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Handler HTTP personalizzato che gestisce automaticamente l'autenticazione con JWT
/// Verifica la validità del token, esegue il refresh se necessario e gestisce gli errori
/// </summary>
public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthenticatedHttpMessageHandler> _logger;

    public AuthenticatedHttpMessageHandler(
        IAuthService authService,
        ILogger<AuthenticatedHttpMessageHandler> logger)
    {
        _authService = authService;
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
        await AddAuthenticationHeader(request,false, cancellationToken);

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
        // Ottieni un token valido da AuthService
        var tokenResult = await _authService.GetValidAccessTokenAsync();
        
        if (tokenResult.IsSuccess)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);
        }
        else
        {
            _logger.LogWarning("Impossibile ottenere un token valido: {Error}", tokenResult.Message);
        }
    }
}