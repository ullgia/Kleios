using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// AuthenticationStateProvider semplice per Blazor SSR (Static Server-Side Rendering)
/// Legge lo stato di autenticazione da HttpContext.User che è già popolato dal Cookie middleware
/// </summary>
public class ServerCookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ServerCookieAuthenticationStateProvider> _logger;

    public ServerCookieAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ServerCookieAuthenticationStateProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // Se HttpContext.User è autenticato, restituiscilo
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogDebug("User authenticated: {UserName} with {ClaimsCount} claims", 
                httpContext.User.Identity.Name, 
                httpContext.User.Claims.Count());
            
            return Task.FromResult(new AuthenticationState(httpContext.User));
        }
        
        // Altrimenti restituisci un utente anonimo
        _logger.LogDebug("User not authenticated, returning anonymous principal");
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(anonymous));
    }
}
