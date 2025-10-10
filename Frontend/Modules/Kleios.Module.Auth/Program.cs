using Kleios.Module.Auth.Components;
using Kleios.Module.Auth.Services;
using Kleios.Module.Auth.Middleware;
using Kleios.Frontend.Infrastructure.Authentication;
using Kleios.Frontend.Infrastructure.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication services
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddKleiosCookieAuthentication(isDevelopment, null);
builder.Services.AddCascadingAuthenticationState();

// Add infrastructure services (IAuthenticationService, etc.)
var backendUrl = builder.Configuration["BackendUrl"] ?? "https://localhost:7000";
builder.Services.AddKleiosInfrastructure(backendUrl);

// Add Gateway registration as hosted service
builder.Services.AddHostedService<AuthModuleRegistration>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Path rewrite middleware - rimuove /auth dal path
app.UsePathRewrite("/auth");

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

// Health check endpoint per il Gateway
app.MapGet("/_health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "auth-module",
    timestamp = DateTime.UtcNow 
}));

app.Run();
