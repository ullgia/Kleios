using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Aggiungi i servizi backend
var authService = builder.AddProject<Kleios_Backend_Authentication>("auth-service");

// Aggiungi il servizio di amministrazione di sistema
var systemService = builder.AddProject<Kleios_Backend_SystemAdmin>("system-service");

// Aggiungi il frontend Blazor
var frontendHost = builder.AddProject<Kleios_Host>("frontend-host")
    .WithReference(authService)
    .WithReference(systemService);

builder.Build().Run();
