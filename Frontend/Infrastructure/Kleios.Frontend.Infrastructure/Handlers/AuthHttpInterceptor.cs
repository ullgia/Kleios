// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Handlers\AuthHttpInterceptor.cs
using System.Net;
using System.Net.Http.Headers;
using Kleios.Frontend.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Handlers;

/// <summary>
/// Interceptor HTTP che gestisce automaticamente l'autenticazione e il refresh dei token
/// </summary>
public class AuthHttpInterceptor : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isRefreshing = false;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<AuthHttpInterceptor> _logger;

    public AuthHttpInterceptor(IServiceProvider serviceProvider, ILogger<AuthHttpInterceptor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

        return await base.SendAsync(request, cancellationToken);
    }

}