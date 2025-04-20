using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kleios.Database.Seeds;

/// <summary>
/// Classe per il seeding del database con dati iniziali
/// </summary>
public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Esegue il seeding iniziale del database se necessario
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Inizio del processo di seeding del database");

            // Utilizza uno scope temporaneo per risolvere i manager di Identity
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var context = scope.ServiceProvider.GetRequiredService<KleiosDbContext>();

            // Verifica se il seeding è già stato eseguito
            if (await userManager.Users.AnyAsync())
            {
                _logger.LogInformation("Seeding saltato: il database contiene già utenti");
                return;
            }

            // Step 1: Crea i permessi di base
            var permissions = await SeedPermissionsAsync(context);
            _logger.LogInformation("Permessi creati con successo");

            // Step 2: Crea i ruoli di base
            var adminRole = await SeedRolesAsync(roleManager);
            _logger.LogInformation("Ruoli creati con successo");

            // Step 3: Assegna i permessi ai ruoli
            await AssignPermissionsToRolesAsync(context, adminRole.Id, permissions);
            _logger.LogInformation("Permessi assegnati ai ruoli con successo");

            // Step 4: Crea gli utenti di default
            await SeedUsersAsync(userManager, roleManager);
            _logger.LogInformation("Utenti creati con successo");

            _logger.LogInformation("Seeding del database completato con successo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Si è verificato un errore durante il seeding del database");
            throw;
        }
    }

    /// <summary>
    /// Crea i permessi di sistema di base
    /// </summary>
    private async Task<List<Permission>> SeedPermissionsAsync(DbContext context)
    {
        // Ottenere tutti i permessi definiti nella classe AppPermissions di Kleios.Shared
        var permissionInfos = PermissionHelper.GetAllPermissions();
        _logger.LogInformation("Trovati {Count} permessi definiti nell'applicazione", permissionInfos.Count);
        
        var permissions = new List<Permission>();
        
        foreach (var permissionInfo in permissionInfos)
        {
            permissions.Add(new Permission
            {
                Name = permissionInfo.Name,
                SystemName = permissionInfo.Value,
                Description = permissionInfo.Description,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Aggiungi i permessi al contesto
        context.Set<Permission>().AddRange(permissions);
        await context.SaveChangesAsync();

        return permissions;
    }

    /// <summary>
    /// Crea i ruoli di sistema di base
    /// </summary>
    private async Task<ApplicationRole> SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        // Ruolo Amministratore
        var adminRole = new ApplicationRole
        {
            Name = SeedingConstants.Roles.Administrator,
            NormalizedName = SeedingConstants.Roles.Administrator.ToUpper(),
            Description = "Ruolo con tutti i permessi",
            IsSystemRole = true,
            CreatedAt = DateTime.UtcNow
        };

        // Ruolo Utente
        var userRole = new ApplicationRole
        {
            Name = SeedingConstants.Roles.User,
            NormalizedName = SeedingConstants.Roles.User.ToUpper(),
            Description = "Ruolo con permessi base",
            IsSystemRole = true,
            CreatedAt = DateTime.UtcNow
        };

        // Crea i ruoli se non esistono
        if (!await roleManager.RoleExistsAsync(adminRole.Name))
        {
            await roleManager.CreateAsync(adminRole);
            _logger.LogInformation("Ruolo '{Role}' creato con successo", adminRole.Name);
        }

        if (!await roleManager.RoleExistsAsync(userRole.Name))
        {
            await roleManager.CreateAsync(userRole);
            _logger.LogInformation("Ruolo '{Role}' creato con successo", userRole.Name);
        }

        // Ottieni il ruolo admin appena creato per ottenere l'ID generato
        return (await roleManager.FindByNameAsync(SeedingConstants.Roles.Administrator))!;
    }

    /// <summary>
    /// Assegna i permessi ai ruoli
    /// </summary>
    private async Task AssignPermissionsToRolesAsync(DbContext context, Guid adminRoleId, List<Permission> permissions)
    {
        // Ottieni il ruolo utente
        var userRole = await context.Set<ApplicationRole>().FirstAsync(r => r.Name == SeedingConstants.Roles.User);

        // Assegna tutti i permessi al ruolo Amministratore
        var adminPermissions = permissions.Select(p => new RolePermission
        {
            RoleId = adminRoleId,
            PermissionId = p.Id
        }).ToList();

        // Assegna solo i permessi di visualizzazione al ruolo Utente
        var viewPermissions = permissions
            .Where(p => p.SystemName.EndsWith(".View"))
            .Select(p => new RolePermission
            {
                RoleId = userRole.Id,
                PermissionId = p.Id
            }).ToList();

        // Aggiungi le relazioni al contesto
        context.Set<RolePermission>().AddRange(adminPermissions);
        context.Set<RolePermission>().AddRange(viewPermissions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Crea gli utenti di default
    /// </summary>
    private async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        // Utente master
        var masterUser = new ApplicationUser
        {
            UserName = SeedingConstants.Users.Master.Username,
            NormalizedUserName = SeedingConstants.Users.Master.Username.ToUpper(),
            Email = SeedingConstants.Users.Master.Email,
            NormalizedEmail = SeedingConstants.Users.Master.Email.ToUpper(),
            EmailConfirmed = true,
            FirstName = SeedingConstants.Users.Master.FirstName,
            LastName = SeedingConstants.Users.Master.LastName,
            IsMasterUser = true,
            CreatedAt = DateTime.UtcNow
        };

        // Utente regolare
        var regularUser = new ApplicationUser
        {
            UserName = SeedingConstants.Users.Regular.Username,
            NormalizedUserName = SeedingConstants.Users.Regular.Username.ToUpper(),
            Email = SeedingConstants.Users.Regular.Email,
            NormalizedEmail = SeedingConstants.Users.Regular.Email.ToUpper(),
            EmailConfirmed = true,
            FirstName = SeedingConstants.Users.Regular.FirstName,
            LastName = SeedingConstants.Users.Regular.LastName,
            CreatedAt = DateTime.UtcNow
        };

        // Crea l'utente master se non esiste
        if (await userManager.FindByNameAsync(masterUser.UserName) == null)
        {
            var result = await userManager.CreateAsync(masterUser, SeedingConstants.Users.Master.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Utente master creato con successo");
                await userManager.AddToRoleAsync(masterUser, SeedingConstants.Roles.Administrator);
                _logger.LogInformation("Utente master assegnato al ruolo '{Role}'", SeedingConstants.Roles.Administrator);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Errore durante la creazione dell'utente master: {Errors}", errors);
            }
        }

        // Crea l'utente regolare se non esiste
        if (await userManager.FindByNameAsync(regularUser.UserName) == null)
        {
            var result = await userManager.CreateAsync(regularUser, SeedingConstants.Users.Regular.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Utente regolare creato con successo");
                await userManager.AddToRoleAsync(regularUser, SeedingConstants.Roles.User);
                _logger.LogInformation("Utente regolare assegnato al ruolo '{Role}'", SeedingConstants.Roles.User);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Errore durante la creazione dell'utente regolare: {Errors}", errors);
            }
        }
    }
}