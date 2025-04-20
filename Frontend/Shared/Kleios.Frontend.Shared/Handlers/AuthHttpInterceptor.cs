// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Modules\Kleios.Modules.Auth\Handlers\AuthHttpInterceptor.cs

using System.Net;
using System.Net.Http.Headers;
using Kleios.Frontend.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kleios.Frontend.Shared.Handlers;

/// <summary>
/// Interceptor HTTP che gestisce automaticamente l'autenticazione e il refresh dei token
/// </summary>
public class AuthHttpInterceptor : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isRefreshing = false;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AuthHttpInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Non aggiungiamo il token per le richieste di autenticazione
        var requestUrl = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (requestUrl.Contains("/auth/login") || 
            requestUrl.Contains("/auth/register") || 
            requestUrl.Contains("/auth/refresh"))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Otteniamo il servizio TokenService
        var tokenService = _serviceProvider.GetRequiredService<ITokenService>();

        // Aggiungi il token di accesso all'header Authorization
        var accessToken = await tokenService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Esegui la richiesta
        var response = await base.SendAsync(request, cancellationToken);

        // Se la richiesta fallisce con 401 Unauthorized, prova il refresh del token
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Utilizziamo un semaforo per evitare refresh multipli simultanei
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isRefreshing)
                {
                    // Se stiamo già facendo il refresh, riutilizziamo il risultato
                    await Task.Delay(500, cancellationToken); // Attendi brevemente
                    
                    // Riprova la richiesta con il nuovo token
                    accessToken = await tokenService.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Crea una nuova richiesta identica alla precedente
                        var newRequest = await CloneHttpRequestMessageAsync(request);
                        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        return await base.SendAsync(newRequest, cancellationToken);
                    }
                }
                else
                {
                    _isRefreshing = true;
                    try
                    {
                        // Creiamo uno scope temporaneo per ottenere l'AuthService
                        // Questo evita la dipendenza circolare
                        using var scope = _serviceProvider.CreateScope();
                        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                        
                        // Tenta di aggiornare il token
                        var refreshResult = await authService.RefreshTokenAsync();
                        
                        if (refreshResult.IsSuccess)
                        {
                            // Riprova la richiesta con il nuovo token
                            accessToken = await tokenService.GetAccessTokenAsync();
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                // Crea una nuova richiesta identica alla precedente
                                var newRequest = await CloneHttpRequestMessageAsync(request);
                                newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                                return await base.SendAsync(newRequest, cancellationToken);
                            }
                        }
                        else
                        {
                            // Se il refresh fallisce, reindirizza alla pagina di login
                            var navigationManager = scope.ServiceProvider.GetRequiredService<NavigationManager>();
                            navigationManager.NavigateTo("/Account/login", forceLoad: true);
                        }
                    }
                    finally
                    {
                        _isRefreshing = false;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return response;
    }

    /// <summary>
    /// Clona una richiesta HTTP per poterla riutilizzare
    /// </summary>
    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copia gli header
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copia le proprietà
        foreach (var property in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
        }

        // Copia il contenuto se presente
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            
            // Copia gli header del contenuto
            if (request.Content.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return clone;
    }
}