using Kleios.Database.Extensions;
using Kleios.ServiceDefaults;
using Kleios.Shared.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using Kleios.Backend.Authentication.Services;
using Kleios.Backend.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.AddKleiosValidation();
builder.Services.AddDatabaseSeeder();
builder.Services.AddKleiosDatabase(useInMemoryDatabase:true);

// Registra i servizi di configurazione e autenticazione
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IConfigurationManagerService, ConfigurationManagerService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Inizializza il servizio di configurazione per ottenere i parametri JWT dal database
var serviceProvider = builder.Services.BuildServiceProvider();
var configManager = serviceProvider.GetRequiredService<IConfigurationManagerService>();
configManager.InitializeAsync().GetAwaiter().GetResult();
var jwtConfig = configManager.GetJwtConfig();

// Configura l'autenticazione JWT con i parametri dal database
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = jwtConfig.GetSigningKey(),
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

// Configura l'autorizzazione
builder.Services.AddAuthorization(options =>
{
    var nestedTypes = typeof(AppPermissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

    foreach (var nestedType in nestedTypes)
    {
        // Ottiene tutti i campi costanti di tipo string nella classe
        var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var propertyValue = field.GetValue(null);
            if (propertyValue is not null)
            {
                options.AddPolicy(propertyValue.ToString()!, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(ApplicationClaimTypes.Permission, propertyValue.ToString()!));
            }
        }
    }
});

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
