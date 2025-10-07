using Kleios.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kleios.Database.Configurations;

/// <summary>
/// Configurazione Entity Framework per AuditLog
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(a => a.ResourceType)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(a => a.ResourceId)
            .HasMaxLength(256);
        
        builder.Property(a => a.Username)
            .HasMaxLength(256);
        
        builder.Property(a => a.Description)
            .HasMaxLength(1000);
        
        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);
        
        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);
        
        builder.Property(a => a.Result)
            .HasMaxLength(20);
        
        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(1000);
        
        builder.Property(a => a.Timestamp)
            .IsRequired();
        
        // Indici per performance
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.ResourceType);
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => new { a.ResourceType, a.ResourceId });
    }
}
