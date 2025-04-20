using Kleios.Database.Seeds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kleios.Database.Extensions;

/// <summary>
/// Estensioni per la configurazione del seed del database
/// </summary>
public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Configura e registra il seeder del database
    /// </summary>
    public static IServiceCollection AddDatabaseSeeder(this IServiceCollection services)
    {
        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    /// <summary>
    /// Esegue il seeding del database usando l'istanza registrata di DatabaseSeeder
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
            
            logger.LogInformation("Avvio del processo di seeding del database...");
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<DatabaseSeeder>>();
            logger?.LogError(ex, "Si è verificato un errore durante il seeding del database");
            throw;
        }
    }
}