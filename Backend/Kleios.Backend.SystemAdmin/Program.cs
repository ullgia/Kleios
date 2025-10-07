using Kleios.Database.Extensions;
using Kleios.ServiceDefaults;
using Kleios.Backend.Shared;
using Kleios.Backend.SharedInfrastructure;
using Kleios.Backend.SharedInfrastructure.Authorization;
using Kleios.Backend.SharedInfrastructure.Cors;
using Kleios.Backend.SharedInfrastructure.Swagger;
using Kleios.Backend.SystemAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddKleiosSwagger(
    "SystemAdmin",
    "API per la gestione amministrativa del sistema Kleios"
);

builder.Services.AddKleiosCors(builder.Configuration);

builder.AddServiceDefaults();
builder.AddKleiosValidation();
builder.Services.AddDatabaseSeeder();
builder.Services.AddSharedInfrastructure();

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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISettingsManagerService, SettingsManagerService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
builder.Services.AddScoped<ISessionManagementService, SessionManagementService>();

builder.Services.AddKleiosAuthorization();

builder.Services.AddKleiosHealthChecks(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseKleiosSwaggerUI("SystemAdmin");
}

app.MigrateKleiosDatabase();
await app.Services.SeedDatabaseAsync();

app.UseHttpsRedirection();

app.UseKleiosInfrastructure(builder.Configuration);

app.MapKleiosHealthChecks();

app.MapControllers();

app.Run();
