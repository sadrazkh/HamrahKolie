using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> b)
    {
        b.ToTable("MediaFiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        b.Property(x => x.StoredPath).HasMaxLength(500).IsRequired();
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        b.Property(x => x.Alt).HasMaxLength(300);
        b.Property(x => x.Caption).HasMaxLength(500);
        b.HasIndex(x => x.CreatedAt);
    }
}

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> b)
    {
        b.ToTable("Menus");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Location).HasConversion<int>();
        b.HasIndex(x => x.Location);
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.ToTable("MenuItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();

        b.HasOne(x => x.Menu).WithMany(m => m.Items).HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Parent).WithMany(p => p.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
