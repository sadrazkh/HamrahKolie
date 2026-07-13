using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class VolunteerConfiguration : IEntityTypeConfiguration<Volunteer>
{
    public void Configure(EntityTypeBuilder<Volunteer> b)
    {
        b.ToTable("Volunteers");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Province).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.CollaborationType).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Skills).HasMaxLength(1000);
        b.Property(x => x.AvailableTimes).HasMaxLength(500);
        b.Property(x => x.Background).HasColumnType("text");
        b.Property(x => x.AdminNotes).HasColumnType("text");
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.Mobile);
    }
}

public class DialysisCenterConfiguration : IEntityTypeConfiguration<DialysisCenter>
{
    public void Configure(EntityTypeBuilder<DialysisCenter> b)
    {
        b.ToTable("DialysisCenters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(280).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Province).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.WorkingHours).HasMaxLength(300);
        b.Property(x => x.Services).HasMaxLength(1000);
        b.Property(x => x.Facilities).HasMaxLength(1000);
        b.Property(x => x.DialysisTypes).HasMaxLength(300);
        b.Property(x => x.AccessibilityNotes).HasMaxLength(500);
        b.Property(x => x.Website).HasMaxLength(300);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.Province);
        b.HasIndex(x => x.IsApproved);
    }
}
