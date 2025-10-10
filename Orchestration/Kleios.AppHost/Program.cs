using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// ========================================
// BACKEND SERVICES
// ========================================
// Note: Porte assegnate dinamicamente da Aspire per evitare conflitti
// Le porte possono essere forzate in Development con variabili d'ambiente

var authBackend = builder.AddProject<Kleios_Backend_Authentication>("auth-backend")
    .WithHttpsEndpoint(name: "auth-backend-https");

var systemBackend = builder.AddProject<Kleios_Backend_SystemAdmin>("system-backend")
    .WithHttpsEndpoint(name: "system-backend-https");

// ========================================
// FRONTEND MODULES
// ========================================
// Note: I moduli frontend comunicano con i backend via service discovery
// Aspire inietta automaticamente la configurazione tramite .WithReference()

var authModule = builder.AddProject<Kleios_Module_Auth>("auth-module")
    .WithHttpsEndpoint(name: "auth-module-https")
    .WithReference(authBackend); // Service discovery: "https+http://auth-backend"

// ========================================
// GATEWAY (YARP Reverse Proxy)
// ========================================
// Note: Gateway mantiene porta fissa 5000 per accesso esterno
// Tutti gli altri servizi usano porte dinamiche e comunicano via service discovery

var gateway = builder.AddProject<Kleios_Gateway>("gateway")
    .WithHttpsEndpoint(port: 5000, name: "gateway-https")
    .WithReference(authBackend)
    .WithReference(systemBackend)
    .WithReference(authModule);

builder.Build().Run();
