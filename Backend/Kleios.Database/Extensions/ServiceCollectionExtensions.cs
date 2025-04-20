using Kleios.Database.Context;
using Kleios.Database.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kleios.Database.Extensions;

/// <summary>
/// Fornisce metodi di estensione per configurare i servizi del database
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Aggiunge il contesto del database comune ai servizi
    /// </summary>
    /// <param name="services">La collezione di servizi</param>
    /// <param name="connectionString">La stringa di connessione al database SQL Server</param>
    /// <param name="useInMemoryDatabase">Se true, utilizza un database in-memory (utile per i test)</param>
    /// <returns>La collezione di servizi aggiornata</returns>
    public static IServiceCollection AddKleiosDatabase(
        this IServiceCollection services, 
        string? connectionString = null, 
        bool useInMemoryDatabase = false)
    {
        if (useInMemoryDatabase)
        {
            services.AddDbContext<KleiosDbContext>(options =>
                options.UseInMemoryDatabase("KleiosDb"));
        }
        else if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<KleiosDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            throw new ArgumentException("Per utilizzare un database SQL Server, devi fornire una stringa di connessione valida");
        }

        // Configura Identity
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Impostazioni delle policy delle password
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                
                // Altre impostazioni di Identity
                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<KleiosDbContext>();

        return services;
    }

    /// <summary>
    /// Applica le migrazioni pendenti al database all'avvio dell'applicazione
    /// </summary>
    /// <param name="app">L'applicazione web</param>
    /// <returns>L'applicazione web</returns>
    public static WebApplication MigrateKleiosDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<KleiosDbContext>();
            
        // Verifica se il database è in-memory (in tal caso, non serve applicare migrazioni)
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            Console.WriteLine("Database in-memory rilevato. Le migrazioni non vengono applicate per i database in-memory.");
            return app;
        }
            
        // Verifica se ci sono migrazioni da applicare
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Applicazione delle migrazioni in corso...");
            dbContext.Database.Migrate();
            Console.WriteLine("Migrazioni applicate con successo");
        }
        else
        {
            Console.WriteLine("Il database è già aggiornato, nessuna migrazione da applicare");
        }

        return app;
    }
}