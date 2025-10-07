using Kleios.Database.Extensions;
using Kleios.ServiceDefaults;
using Kleios.Backend.Authentication.Services;
using Kleios.Backend.Shared;
using Kleios.Backend.SharedInfrastructure;
using Kleios.Backend.SharedInfrastructure.Authorization;
using Kleios.Backend.SharedInfrastructure.Cors;
using Kleios.Backend.SharedInfrastructure.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddKleiosSwagger(
    "Authentication",
    "API per l autenticazione e gestione token JWT del sistema Kleios"
);

builder.Services.AddKleiosCors(builder.Configuration);

builder.AddServiceDefaults();
builder.AddKleiosValidation();
builder.Services.AddDatabaseSeeder();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useInMemory = string.IsNullOrEmpty(connectionString);

if (useInMemory)
{
    builder.Services.AddKleiosDatabase(useInMemoryDatabase: true);
}
else
{
    builder.Services.AddKleiosDatabase(connectionString: connectionString);
}

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddKleiosAuthorization();

builder.Services.AddKleiosHealthChecks(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseKleiosSwaggerUI("Authentication");
}

app.MigrateKleiosDatabase();
await app.Services.SeedDatabaseAsync();

app.UseHttpsRedirection();

app.UseKleiosInfrastructure(builder.Configuration);

app.MapKleiosHealthChecks();

app.MapControllers();

app.Run();
