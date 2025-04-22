using Kleios.Backend.SystemAdmin.Services;
using Kleios.Database.Extensions;
using Kleios.Security.Extensions;
using Kleios.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.AddKleiosValidation();
builder.Services.AddKleiosSecurity(builder.Configuration);

// Registra i servizi del modulo System
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Add Controllers
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Applica le migrazioni all'avvio dell'applicazione
app.MigrateKleiosDatabase();

app.UseHttpsRedirection();

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
