using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Aggiungi i servizi backend
var authService = builder.AddProject<Kleios_Backend_Authentication>("auth-service");

var logsSettingsService = builder.AddProject<Kleios_Backend_LogsSettings>("logs-settings-service");

// Aggiungi il frontend Blazor
var frontendHost = builder.AddProject<Kleios_Host>("frontend-host")
    .WithReference(authService)
    .WithReference(logsSettingsService);

builder.Build().Run();
