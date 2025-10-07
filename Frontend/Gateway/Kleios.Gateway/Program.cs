using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Kleios.Shared;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// RATE LIMITING (Protezione DDoS)
// ==========================================
builder.Services.AddRateLimiter(options =>
{
    // Policy per endpoint di autenticazione (più restrittiva)
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(KleiosConstants.RateLimiting.WindowMinutes);
        opt.PermitLimit = KleiosConstants.RateLimiting.AuthenticationPermitLimit;
        opt.QueueLimit = 0;     // Nessuna coda
    });
    
    // Policy per API generali
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(KleiosConstants.RateLimiting.WindowMinutes);
        opt.PermitLimit = KleiosConstants.RateLimiting.ApiPermitLimit;
        opt.QueueLimit = 5;      // Coda di 5 richieste
    });
    
    // Policy default per frontend (più permissiva)
    options.AddFixedWindowLimiter("default", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(KleiosConstants.RateLimiting.WindowMinutes);
        opt.PermitLimit = KleiosConstants.RateLimiting.DefaultPermitLimit;
        opt.QueueLimit = 10;     // Coda di 10 richieste
    });
    
    // Gestione del rejection
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429; // Too Many Requests
        await context.HttpContext.Response.WriteAsync(
            "Rate limit superato. Riprova tra qualche istante.", 
            cancellationToken: token);
    };
});

// ==========================================
// YARP REVERSE PROXY (Gateway puro)
// ==========================================
// Il Gateway è solo un reverse proxy, non valida JWT
// L'autenticazione è gestita da:
// - Cookie Authentication nei frontend SSR (per HttpContext.User)
// - JWT Bearer nei backend API (per Authorization header)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Middleware pipeline (ordine importante!)
app.UseHttpsRedirection();

// Rate Limiting middleware (prima del proxy)
app.UseRateLimiter();

// Map reverse proxy con rate limiting policies
app.MapReverseProxy()
    .RequireRateLimiting("default");

app.Run();

