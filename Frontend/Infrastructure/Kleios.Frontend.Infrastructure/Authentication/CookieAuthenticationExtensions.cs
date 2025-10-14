using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Kleios.Frontend.Shared;

namespace Kleios.Frontend.Infrastructure.Authentication;

/// <summary>
/// Extension methods per configurare Cookie Authentication in modo unificato
/// </summary>
public static class CookieAuthenticationExtensions
{
    /// <summary>
    /// Aggiunge e configura Cookie Authentication con le impostazioni standard Kleios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="isDevelopment">Se true, usa configurazione development (domain: null)</param>
    /// <param name="productionDomain">Dominio per production (opzionale)</param>
    public static IServiceCollection AddKleiosCookieAuthentication(
        this IServiceCollection services,
        bool isDevelopment = true,
        string? productionDomain = null)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                // Nome cookie condiviso
                options.Cookie.Name = KleiosConstants.AuthCookieName;

                // Domain configuration
                // CRITICO: null in development per localhost, altrimenti non funziona!
                options.Cookie.Domain = isDevelopment ? null : productionDomain;

                // Path condiviso tra tutti i moduli
                options.Cookie.Path = "/";

                // SameSite DEVE essere Lax per permettere redirect tra moduli
                // Non usare Strict altrimenti i redirect perdono il cookie
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

                // HttpOnly per sicurezza (no access da JavaScript)
                options.Cookie.HttpOnly = true;

                // Secure solo in HTTPS (development usa certificati dev)
                options.Cookie.SecurePolicy = isDevelopment
                    ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
                    : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;

                // Expiration
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;

                // Paths per authentication
                // Internally the modules expose Account routes (standalone)
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";

                // Override default redirect behavior so that when a module issues a challenge
                // it redirects the client to the Gateway's auth endpoints (so auth is centralised).
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx =>
                    {
                        var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
                        var loginPath = KleiosConstants.AuthPaths.Login + "?returnUrl=" + returnUrl;
                        ctx.Response.StatusCode = StatusCodes.Status302Found;
                        ctx.Response.Headers["Location"] = loginPath;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = ctx =>
                    {
                        var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
                        var accessDeniedPath = KleiosConstants.AuthPaths.AccessDenied + "?returnUrl=" + returnUrl;
                        ctx.Response.StatusCode = StatusCodes.Status302Found;
                        ctx.Response.Headers["Location"] = accessDeniedPath;
                        return Task.CompletedTask;
                    },
                    OnRedirectToLogout = ctx =>
                    {
                        // Logout should point to gateway logout so the cookie can be cleared centrally
                        var logoutPath = KleiosConstants.AuthPaths.Logout;
                        ctx.Response.StatusCode = StatusCodes.Status302Found;
                        ctx.Response.Headers["Location"] = logoutPath;
                        return Task.CompletedTask;
                    }
                };

                // Return URL parameter
                options.ReturnUrlParameter = "returnUrl";
            });

        return services;
    }
}
