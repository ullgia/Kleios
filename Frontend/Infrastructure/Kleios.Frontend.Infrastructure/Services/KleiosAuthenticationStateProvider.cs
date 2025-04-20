using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Kleios.Frontend.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Provider per lo stato di autenticazione che utilizza UserInfoState
/// per funzionare correttamente anche durante il prerendering server
/// </summary>
public class KleiosAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITokenService _tokenService;
    private readonly UserInfoState _userInfoState;

    public KleiosAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        ITokenService tokenService,
        UserInfoState userInfoState) : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _tokenService = tokenService;
        _userInfoState = userInfoState;
    }

    /// <summary>
    /// Ottiene lo stato di autenticazione corrente
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        
        // Se l'utente è già autenticato nello stato scoped, utilizza quelle informazioni
        if (_userInfoState.IsAuthenticated)
        {
            identity = new ClaimsIdentity(_userInfoState.Claims, "jwt");
        }
        // Altrimenti verifica se c'è un refresh token valido e prova a ottenere un nuovo access token
        else if (await _tokenService.HasValidTokenAsync())
        {
            // Prima controlla se c'è un access token valido
            var accessToken = await _tokenService.GetAccessTokenAsync();
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    // Analizza il token JWT per estrarre le informazioni dell'utente
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(accessToken);
                    
                    // Aggiorna lo stato dell'utente con il token e i claim
                    _userInfoState.UpdateFromToken(accessToken, jwtToken.Claims);
                    
                    // Crea l'identità con i claim
                    identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
                }
                catch
                {
                    // In caso di errore nell'analisi del token, continua con un'identità vuota
                }
            }
        }
        
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>
    /// Aggiorna lo stato con un nuovo token JWT
    /// </summary>
    public void UpdateAuthenticationState(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            _userInfoState.Reset();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal())));
            return;
        }

        try
        {
            // Analizza il token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            // Aggiorna lo stato dell'utente
            _userInfoState.UpdateFromToken(accessToken, jwtToken.Claims);

            // Notifica il cambiamento dello stato di autenticazione
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
        catch
        {
            // In caso di errore, resetta lo stato dell'utente
            _userInfoState.Reset();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal())));
        }
    }

    /// <summary>
    /// Notifica un cambiamento dello stato di autenticazione
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    protected override TimeSpan RevalidationInterval { get; } = TimeSpan.FromMinutes(30);
}