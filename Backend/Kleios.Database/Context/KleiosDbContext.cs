using Kleios.Database.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kleios.Database.Context;

/// <summary>
/// Contesto del database comune per tutti i progetti del backend
/// </summary>
public class KleiosDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, 
    IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>, 
    IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    public KleiosDbContext(DbContextOptions<KleiosDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configura chiavi composte per le relazioni molti-a-molti
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });
        
        // Le relazioni tra entità sono configurate qui
        // Il seeding è stato spostato nella classe DatabaseSeeder
    }
}