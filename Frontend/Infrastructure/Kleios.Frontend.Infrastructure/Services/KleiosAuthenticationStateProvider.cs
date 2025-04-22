using System.Security.Claims;
using Kleios.Frontend.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Provider per lo stato di autenticazione
/// </summary>
public class KleiosAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly ILogger<KleiosAuthenticationStateProvider> _logger;
    private readonly IOptions<IdentityOptions> _options;
    private readonly IAuthService _authService;
    
    private static AuthenticationState _anonymousAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

    // Stato dell'autenticazione attuale
    private AuthenticationState _currentAuthenticationState;

    public KleiosAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        ILogger<KleiosAuthenticationStateProvider> logger,
        IOptions<IdentityOptions> options,
        IAuthService authService)
        : base(loggerFactory)
    {
        _logger = logger;
        _options = options;
        _authService = authService;
        
        _currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        try
        {
            // Verifica lo security stamp
            return await ValidateSecurityStampAsync(authenticationState.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la validazione dello stato di autenticazione");
            return false;
        }
    }

    private async Task<bool> ValidateSecurityStampAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            _logger.LogDebug("Validazione fallita: nessun ID utente trovato");
            return false;
        }

        var securityStampResult = await _authService.GetSecurityStampAsync();
        if (!securityStampResult.IsSuccess)
        {
            _logger.LogDebug("Validazione fallita: security stamp non disponibile");
            return false;
        }

        var principalStamp = principal.FindFirstValue(_options.Value.ClaimsIdentity.SecurityStampClaimType);
        var userStamp = securityStampResult.Value;
        
        var isValid = string.Equals(principalStamp, userStamp, StringComparison.Ordinal);
        _logger.LogDebug("Validazione security stamp: {IsValid}", isValid);
        
        return isValid;
    }
}