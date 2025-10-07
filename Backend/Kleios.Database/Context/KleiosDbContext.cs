using Kleios.Database.Configurations;
using Kleios.Database.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Kleios.Database.Context;

/// <summary>
/// Contesto del database comune per tutti i progetti del backend
/// </summary>
public class KleiosDbContext : IdentityDbContext<
    ApplicationUser,                   // TUser
    ApplicationRole,                   // TRole
    Guid,                             // TKey
    ApplicationUserClaim,             // TUserClaim
    ApplicationUserRole,              // TUserRole
    ApplicationUserLogin,             // TUserLogin
    ApplicationRoleClaim,             // TRoleClaim
    ApplicationUserToken>             // TUserToken - ora usiamo la nostra classe personalizzata
{
    public KleiosDbContext(DbContextOptions<KleiosDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<FailedLoginAttempt> FailedLoginAttempts { get; set; }
    public DbSet<BlockedIp> BlockedIps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Applica tutte le configurazioni IEntityTypeConfiguration definite nell'assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}