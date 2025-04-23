using Kleios.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;

namespace Kleios.Database.Configurations;

/// <summary>
/// Configurazione per l'entità ApplicationUser
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        // Configurazioni aggiuntive
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        
        // Configurazione per i refresh token
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configurazione per l'entità ApplicationRole
/// </summary>
public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");

        // Configurazioni aggiuntive
        builder.Property(r => r.Description).HasMaxLength(256);
    }
}

/// <summary>
/// Configurazione per l'entità ApplicationUserClaim
/// </summary>
public class ApplicationUserClaimConfiguration : IEntityTypeConfiguration<ApplicationUserClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationUserClaim> builder)
    {
        builder.ToTable("UserClaims");

        // Configurazione della relazione
        builder.HasOne(uc => uc.User)
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configurazione per l'entità ApplicationUserRole
/// </summary>
public class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
    {
        builder.ToTable("UserRoles");

        // Configurazione delle relazioni
        builder.HasOne(ur => ur.User)
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configurazione per l'entità ApplicationUserLogin
/// </summary>
public class ApplicationUserLoginConfiguration : IEntityTypeConfiguration<ApplicationUserLogin>
{
    public void Configure(EntityTypeBuilder<ApplicationUserLogin> builder)
    {
        builder.ToTable("UserLogins");

        // Configurazione della relazione
        builder.HasOne(ul => ul.User)
            .WithMany()
            .HasForeignKey(ul => ul.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            
        // Configurazioni aggiuntive
        builder.Property(ul => ul.LastIpAddress).HasMaxLength(50);
    }
}

/// <summary>
/// Configurazione per l'entità ApplicationRoleClaim
/// </summary>
public class ApplicationRoleClaimConfiguration : IEntityTypeConfiguration<ApplicationRoleClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
    {
        builder.ToTable("RoleClaims");

        // Configurazione della relazione
        builder.HasOne(rc => rc.Role)
            .WithMany()
            .HasForeignKey(rc => rc.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configurazione per ApplicationUserToken
/// </summary>
public class ApplicationUserTokenConfiguration : IEntityTypeConfiguration<ApplicationUserToken>
{
    public void Configure(EntityTypeBuilder<ApplicationUserToken> builder)
    {
        builder.ToTable("IdentityUserTokens");

        // Configurazione delle proprietà
        builder.Property(ut => ut.IpAddress).HasMaxLength(50);
        builder.Property(ut => ut.UserAgent).HasMaxLength(512);
        
        // Configurazione della relazione
        builder.HasOne(ut => ut.User)
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}