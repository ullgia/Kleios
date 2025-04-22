using Kleios.Database.Extensions;
using Kleios.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddDatabaseSeeder();
builder.Services.AddKleiosSecurity(builder.Configuration);

// Add Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Applica le migrazioni all'avvio dell'applicazione
app.MigrateKleiosDatabase();

// Esegue il seeding del database
await app.Services.SeedDatabaseAsync();

app.UseHttpsRedirection();

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
