using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class PageSectionConfiguration : IEntityTypeConfiguration<PageSection>
{
    public void Configure(EntityTypeBuilder<PageSection> b)
    {
        b.ToTable("PageSections");
        b.HasKey(x => x.Id);
        b.Property(x => x.PageKey).HasMaxLength(64).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Background).HasConversion<int>();
        b.Property(x => x.Padding).HasConversion<int>();
        b.Property(x => x.Title).HasMaxLength(300);
        b.Property(x => x.Subtitle).HasMaxLength(600);
        b.Property(x => x.Body).HasColumnType("text");
        b.Property(x => x.ButtonText).HasMaxLength(120);
        b.Property(x => x.ButtonUrl).HasMaxLength(500);
        b.Property(x => x.SecondaryButtonText).HasMaxLength(120);
        b.Property(x => x.SecondaryButtonUrl).HasMaxLength(500);
        b.Property(x => x.SettingsJson).HasColumnType("text");

        b.HasIndex(x => new { x.PageKey, x.SortOrder });

        b.HasOne(x => x.Image)
            .WithMany()
            .HasForeignKey(x => x.ImageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
