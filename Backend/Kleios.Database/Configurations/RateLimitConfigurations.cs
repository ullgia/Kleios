using Kleios.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kleios.Database.Configurations;

public class FailedLoginAttemptConfiguration : IEntityTypeConfiguration<FailedLoginAttempt>
{
    public void Configure(EntityTypeBuilder<FailedLoginAttempt> builder)
    {
        builder.ToTable("FailedLoginAttempts");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Username)
            .HasMaxLength(256)
            .IsRequired();
        
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45)
            .IsRequired();
        
        builder.Property(x => x.Reason)
            .HasMaxLength(500);
        
        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);
        
        // Indici per performance
        builder.HasIndex(x => x.IpAddress);
        builder.HasIndex(x => x.Username);
        builder.HasIndex(x => x.AttemptTime);
        builder.HasIndex(x => new { x.IpAddress, x.AttemptTime });
    }
}

public class BlockedIpConfiguration : IEntityTypeConfiguration<BlockedIp>
{
    public void Configure(EntityTypeBuilder<BlockedIp> builder)
    {
        builder.ToTable("BlockedIps");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45)
            .IsRequired();
        
        builder.Property(x => x.Reason)
            .HasMaxLength(500);
        
        // Indici per performance
        builder.HasIndex(x => x.IpAddress).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => new { x.IsActive, x.IpAddress });
    }
}
