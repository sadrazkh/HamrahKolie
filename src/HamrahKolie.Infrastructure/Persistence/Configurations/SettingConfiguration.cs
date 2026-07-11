using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> b)
    {
        b.ToTable("Settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Key).IsUnique();
        b.Property(x => x.Group).HasMaxLength(128).IsRequired();
        b.Property(x => x.Value).HasColumnType("text");
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(128).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(128);
        b.Property(x => x.EntityId).HasMaxLength(128);
        b.Property(x => x.UserId).HasMaxLength(450);
        b.Property(x => x.UserName).HasMaxLength(256);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(512);
        b.Property(x => x.Description).HasMaxLength(1024);
        b.Property(x => x.MetadataJson).HasColumnType("text");
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.Action);
    }
}
