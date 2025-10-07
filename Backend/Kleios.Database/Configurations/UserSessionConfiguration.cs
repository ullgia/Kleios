using Kleios.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kleios.Database.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45)
            .IsRequired();
        
        builder.Property(x => x.UserAgent)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(x => x.DeviceType)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(x => x.Browser)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.OperatingSystem)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.Location)
            .HasMaxLength(200);
        
        builder.Property(x => x.SessionToken)
            .HasMaxLength(256)
            .IsRequired();
        
        // Indici per performance
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SessionToken).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.UserId, x.IsActive });
        
        // Relazione con User
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
