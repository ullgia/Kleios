using Kleios.Frontend.Infrastructure;
using Kleios.Frontend.Infrastructure.Handlers;
using Kleios.Frontend.Infrastructure.Services;
using Kleios.Frontend.Shared.Services;
using Kleios.Host.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using MudBlazor.Services;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Kleios.Shared;
using ZiggyCreatures.Caching.Fusion;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Aggiungi FusionCache
builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromMinutes(5),
        JitterMaxDuration = TimeSpan.FromSeconds(2)
    });

// Aggiungi i servizi di autenticazione
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/login";
        options.LogoutPath = "/Account/logout";
        options.AccessDeniedPath = "/access-denied";
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                var userId = userIdClaim.Value;

                var fusionCache = context.HttpContext.RequestServices.GetRequiredService<IFusionCache>();
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

                var cacheKey = $"User-Authorization-{userId}";

                var principal = await fusionCache.GetOrSetAsync(
                    cacheKey,
                    async _ =>
                    {
                        var principal = await authService.GetUserClaims();

                        if (principal.IsFailure)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme);
                            return new ClaimsPrincipal();
                        }


                        return principal.Value;
                    }, TimeSpan.FromSeconds(30));

                context.ReplacePrincipal(principal);
                context.ShouldRenew = true;

            }
        };
    });

// Aggiungi i servizi di autenticazione standard di Blazor
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, KleiosAuthenticationStateProvider>();

// Aggiungi MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
})
.AddMudPopoverService()
.AddMudBlazorSnackbar(); 

// Aggiungi HttpContextAccessor per il servizio dei token
builder.Services.AddHttpContextAccessor();

// Aggiungi tutti i servizi dell'infrastruttura
builder.Services.AddInfrastructureServices();

// Configura HttpClient per utilizzare l'interceptor globale
builder.Services.AddHttpClient("API", client => {
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001");
}).AddHttpMessageHandler<AuthHttpInterceptor>();

// Configura l'HttpClient di default
builder.Services.AddHttpClient();

// Aggiungi i servizi di default
builder.AddServiceDefaults();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies([typeof(Kleios.Modules.Auth._Imports).Assembly]);

app.Run();


