using Kleios.Modules.Auth.Host.Components;
using Kleios.Frontend.Infrastructure;
using Kleios.Frontend.Infrastructure.Services;
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add HttpContextAccessor (necessario per leggere HttpContext.User)
builder.Services.AddHttpContextAccessor();

// ==========================================
// COOKIE AUTHENTICATION (ASP.NET Core standard)
// ==========================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = KleiosConstants.Authentication.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Solo HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;  // Lax per permettere redirect esterni
        options.ExpireTimeSpan = TimeSpan.FromMinutes(KleiosConstants.Authentication.CookieExpirationMinutes);
        options.SlidingExpiration = true;
        
        // Percorsi gestiti tramite Gateway
        options.LoginPath = "/auth/Account/Login";
        options.LogoutPath = "/auth/Account/Logout";
        options.AccessDeniedPath = "/auth/Account/AccessDenied";
        
        // Eventi per debug e validazione
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogDebug("Validating cookie principal for user: {User}", 
                    context.Principal?.Identity?.Name ?? "Anonymous");
                
                await Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ==========================================
// AUTHENTICATION STATE PROVIDER
// ==========================================
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerCookieAuthenticationStateProvider>();

// ==========================================
// DISTRIBUTED CACHE (per token storage)
// ==========================================
builder.Services.AddDistributedMemoryCache();

// ==========================================
// FUSION CACHE (per caching user claims)
// ==========================================
builder.Services.AddFusionCache();

// ==========================================
// INFRASTRUCTURE SERVICES
// (TokenStore, AuthService, MenuService, ecc.)
// ==========================================
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

// ==========================================
// MIDDLEWARE PIPELINE (ORDINE IMPORTANTE!)
// ==========================================
app.UseAuthentication();  // ← Prima legge cookie e popola HttpContext.User
app.UseAuthorization();   // ← Poi verifica autorizzazioni

// Map Razor Components from this app, shared components, and Auth RCL
app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(
        typeof(Kleios.Frontend.Components.Layout.MainLayout).Assembly,
        typeof(Kleios.Modules.Auth._Imports).Assembly
    );

app.Run();

