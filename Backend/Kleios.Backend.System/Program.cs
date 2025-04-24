using Kleios.Backend.SystemAdmin.Services;
using Kleios.Database.Extensions;
using Kleios.ServiceDefaults;
using Kleios.Shared.Authorization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.AddKleiosValidation();
builder.Services.AddDatabaseSeeder();
builder.Services.AddKleiosDatabase(useInMemoryDatabase: true);
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
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? "Kleios_JWT_Secret_Key_For_Auth_At_Least_32_Characters"))
    };
});

// Registra i servizi del modulo System
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
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

