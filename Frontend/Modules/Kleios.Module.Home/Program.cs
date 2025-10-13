using Kleios.Module.Auth.Services;
using Kleios.Module.Home.Components;
using MudBlazor.Services;

using Kleios.Frontend.Infrastructure.Authentication;
using Kleios.Frontend.Infrastructure.Services;
var builder = WebApplication.CreateBuilder(args);

// Add service discovery (Aspire integration)
builder.Services.AddServiceDiscovery();

// Configure HttpClient for backend with service discovery
builder.Services.ConfigureHttpClientDefaults(http =>
{
    // Turn on resilience by default
    http.AddStandardResilienceHandler();

    // Turn on service discovery by default
    http.AddServiceDiscovery();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication & authorization services
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddKleiosCookieAuthentication(isDevelopment, null);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

// Add infrastructure services (IAuthenticationService, etc.)
// Backend URL viene da Aspire Service Discovery tramite .WithReference() nell'AppHost
// In development: "https+http://auth-backend" (risolto automaticamente)
// In production: configurato in appsettings.json
var backendUrl = builder.Configuration["BackendUrl"] ?? "https+http://auth-backend";
builder.Services.AddKleiosInfrastructure(backendUrl);

// Add Gateway registration as hosted service
builder.Services.AddHostedService<HomeModuleRegistration>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Health check endpoint per il Gateway
app.MapGet("/_health", () => Results.Ok(new
{
    status = "healthy",
    service = "home-module",
    timestamp = DateTime.UtcNow
}));

app.Run();
