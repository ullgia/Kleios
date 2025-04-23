using Kleios.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kleios.Database.Configurations;

/// <summary>
/// Configurazione per l'entità Permission
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        
        // Configurazioni delle proprietà
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(256);
        builder.Property(p => p.Group).HasMaxLength(100);
        
        // Indice univoco sul nome del permesso
        builder.HasIndex(p => p.Name).IsUnique();
    }
}

/// <summary>
/// Configurazione per l'entità RolePermission
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        
        // Chiave primaria composta
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        
        // Configurazione delle relazioni
        builder.HasOne(rp => rp.Role)
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configurazione per l'entità RefreshToken
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        
        // Configurazioni delle proprietà
        builder.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
        builder.Property(rt => rt.JwtId).HasMaxLength(256);
        
        // Configurazione della relazione
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configurazione per l'entità AppSetting
/// </summary>
public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");
        
        // Configurazioni delle proprietà
        builder.Property(s => s.Key).HasMaxLength(128).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(4000);
        builder.Property(s => s.DataType).HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(500);
        
        // Indice univoco sulla chiave
        builder.HasIndex(s => s.Key).IsUnique();
    }
}

/// <summary>
/// Configurazione per l'entità UserToken
/// </summary>
public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");
        
        // Configurazioni delle proprietà
        builder.Property(ut => ut.TokenType).HasMaxLength(50).IsRequired();
        builder.Property(ut => ut.TokenValue).HasMaxLength(4000).IsRequired();
        builder.Property(ut => ut.DeviceId).HasMaxLength(128);
        builder.Property(ut => ut.LastIpAddress).HasMaxLength(50);
        
        // Configurazione della relazione
        builder.HasOne(ut => ut.User)
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}