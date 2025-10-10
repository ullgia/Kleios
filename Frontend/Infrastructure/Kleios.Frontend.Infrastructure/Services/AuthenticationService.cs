using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Kleios.Shared;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione servizio di autenticazione
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public AuthenticationService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<Option<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Email}", request.Email);

            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Login failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                return Option<LoginResponse>.Failure("Login failed", response.StatusCode);
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);

            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                _logger.LogError("Login response is null or token is empty");
                return Option<LoginResponse>.Failure("Invalid login response");
            }

            // Imposta cookie
            SetAuthCookie(loginResponse.Token);

            // Notifica cambio stato autenticazione
            if (_authStateProvider is Authentication.ServerCookieAuthenticationStateProvider provider)
            {
                provider.NotifyAuthenticationStateChanged();
            }

            _logger.LogInformation("Login successful for user: {Email}", request.Email);

            return Option<LoginResponse>.Success(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
            return Option<LoginResponse>.ServerError($"Login error: {ex.Message}");
        }
    }

    public async Task<Option> LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting logout");

            // Rimuovi cookie
            RemoveAuthCookie();

            // Notifica cambio stato autenticazione
            if (_authStateProvider is Authentication.ServerCookieAuthenticationStateProvider provider)
            {
                provider.NotifyAuthenticationStateChanged();
            }

            // Optional: call backend logout endpoint
            try
            {
                await _httpClient.PostAsync("/api/auth/logout", null, cancellationToken);
            }
            catch
            {
                // Ignore backend logout errors
            }

            _logger.LogInformation("Logout successful");

            return Option.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Option.ServerError($"Logout error: {ex.Message}");
        }
    }

    public async Task<Option<UserInfo>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return Option<UserInfo>.Unauthorized("User not authenticated");
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? user.FindFirst("sub")?.Value 
                         ?? string.Empty;
            var email = user.FindFirst(ClaimTypes.Email)?.Value 
                        ?? user.FindFirst("email")?.Value 
                        ?? string.Empty;
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            var additionalClaims = user.Claims
                .Where(c => c.Type != ClaimTypes.NameIdentifier 
                            && c.Type != "sub" 
                            && c.Type != ClaimTypes.Email 
                            && c.Type != "email" 
                            && c.Type != ClaimTypes.Role)
                .ToDictionary(c => c.Type, c => c.Value);

            var userInfo = new UserInfo(userId, email, roles, additionalClaims);

            return Option<UserInfo>.Success(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return Option<UserInfo>.ServerError($"Error getting current user: {ex.Message}");
        }
    }

    public async Task<Option<bool>> ValidateTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Option<bool>.Success(false);
            }

            var cookieValue = httpContext.Request.Cookies[Frontend.Shared.KleiosConstants.AuthCookieName];

            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                return Option<bool>.Success(false);
            }

            if (!_tokenHandler.CanReadToken(cookieValue))
            {
                return Option<bool>.Success(false);
            }

            var jwtToken = _tokenHandler.ReadJwtToken(cookieValue);

            // Verifica expiration
            var isValid = jwtToken.ValidTo >= DateTime.UtcNow;

            return Option<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Option<bool>.ServerError($"Token validation error: {ex.Message}");
        }
    }

    private void SetAuthCookie(string token)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60),
            Path = "/"
        };

        httpContext.Response.Cookies.Append(Frontend.Shared.KleiosConstants.AuthCookieName, token, cookieOptions);
    }

    private void RemoveAuthCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        httpContext.Response.Cookies.Delete(Frontend.Shared.KleiosConstants.AuthCookieName, new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax
        });
    }
}
