using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Kleios.Frontend.Shared;

namespace Kleios.Frontend.Infrastructure.Authentication;

/// <summary>
/// AuthenticationStateProvider che legge JWT dal cookie server-side
/// </summary>
public class ServerCookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ServerCookieAuthenticationStateProvider> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public ServerCookieAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ServerCookieAuthenticationStateProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, returning anonymous user");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        try
        {
            // Leggi cookie
            var cookieValue = httpContext.Request.Cookies[KleiosConstants.AuthCookieName];

            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                _logger.LogDebug("No auth cookie found, returning anonymous user");
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            // Parse JWT
            if (!_tokenHandler.CanReadToken(cookieValue))
            {
                _logger.LogWarning("Invalid JWT token in cookie");
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            var jwtToken = _tokenHandler.ReadJwtToken(cookieValue);

            // Verifica expiration
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("JWT token expired at {ExpirationTime}", jwtToken.ValidTo);
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            // Crea ClaimsPrincipal dai claims del JWT
            var claims = jwtToken.Claims.ToList();

            // Assicurati che ci sia un name claim (richiesto da ClaimsPrincipal)
            if (!claims.Any(c => c.Type == ClaimTypes.Name || c.Type == "name"))
            {
                var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email");
                if (emailClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, emailClaim.Value));
                }
            }

            // Aggiungi role claims se presenti come array o stringa
            var roleClaims = claims.Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role).ToList();
            foreach (var roleClaim in roleClaims.Where(c => c.Type == "roles"))
            {
                // Se è un array JSON, parsalo
                if (roleClaim.Value.StartsWith("["))
                {
                    try
                    {
                        var roles = System.Text.Json.JsonSerializer.Deserialize<string[]>(roleClaim.Value);
                        if (roles != null)
                        {
                            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
                        }
                    }
                    catch
                    {
                        // Ignora errori parsing
                    }
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            _logger.LogDebug("User authenticated from cookie: {UserName}", user.Identity?.Name ?? "Unknown");

            return Task.FromResult(new AuthenticationState(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading authentication state from cookie");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }
    }

    /// <summary>
    /// Notifica che lo stato di autenticazione è cambiato
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
