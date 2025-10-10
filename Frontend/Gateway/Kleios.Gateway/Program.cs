using Kleios.Gateway.Middleware;
using Kleios.Gateway.Services;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Service Registry e WebSocket Manager
builder.Services.AddSingleton<IServiceRegistry, InMemoryServiceRegistry>();
builder.Services.AddSingleton<IWebSocketManager, Kleios.Gateway.Services.WebSocketManager>();

// Background Service per health monitoring
builder.Services.AddHostedService<ServiceHealthMonitor>();

// HttpClient per health checks
builder.Services.AddHttpClient();

// YARP Reverse Proxy
builder.Services.AddHttpForwarder();

// CORS (se necessario per development)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Serve static files (MudBlazor assets)
app.UseStaticFiles();

// CORS
app.UseCors();

// WebSocket endpoint per heartbeat
app.Map("/ws/{serviceName}", async (HttpContext context, string serviceName, IWebSocketManager wsManager) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await wsManager.HandleWebSocketAsync(webSocket, serviceName, context.RequestAborted);
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket connection required");
    }
});

// Gateway API Controllers
app.MapControllers();

// Custom Prefix Routing Middleware (DEVE essere dopo UseStaticFiles e prima di MapControllers)
app.UsePrefixRouting();

// Health check endpoint per il Gateway stesso
app.MapGet("/_health", () => Results.Ok(new
{
    status = "healthy",
    service = "gateway",
    timestamp = DateTime.UtcNow
}));

// Homepage con info sul Gateway
app.MapGet("/", async (IServiceRegistry serviceRegistry) =>
{
    var services = await serviceRegistry.GetAllServicesAsync();
    var serviceCount = services.Count();
    var healthyCount = services.Count(s => s.IsHealthy);

    return Results.Ok(new
    {
        gateway = "Kleios Gateway",
        version = "1.0.0",
        status = "running",
        timestamp = DateTime.UtcNow,
        statistics = new
        {
            totalServices = serviceCount,
            healthyServices = healthyCount,
            unhealthyServices = serviceCount - healthyCount
        },
        endpoints = new
        {
            registration = "/api/_gateway/register",
            routes = "/api/_gateway/routes",
            services = "/api/_gateway/services",
            websocket = "/ws/{serviceName}",
            health = "/_health"
        }
    });
});

app.Run();
