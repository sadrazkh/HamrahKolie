using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> b)
    {
        b.ToTable("Contents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(320).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(1000);
        b.Property(x => x.Body).HasColumnType("text");
        b.Property(x => x.Language).HasMaxLength(8).IsRequired();
        b.Property(x => x.AuthorId).HasMaxLength(450);
        b.Property(x => x.MedicalReviewer).HasMaxLength(256);

        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();

        // نامک در هر نوع محتوا یکتاست.
        b.HasIndex(x => new { x.Type, x.Slug }).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.PublishedAt);

        b.HasOne(x => x.FeaturedImage)
            .WithMany()
            .HasForeignKey(x => x.FeaturedImageId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Category)
            .WithMany(c => c.Contents)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // سئو به‌صورت Owned در همان جدول.
        b.OwnsOne(x => x.Seo, seo =>
        {
            seo.Property(p => p.SeoTitle).HasColumnName("SeoTitle").HasMaxLength(300);
            seo.Property(p => p.MetaDescription).HasColumnName("MetaDescription").HasMaxLength(500);
            seo.Property(p => p.CanonicalUrl).HasColumnName("CanonicalUrl").HasMaxLength(500);
            seo.Property(p => p.OgImageUrl).HasColumnName("OgImageUrl").HasMaxLength(500);
            seo.Property(p => p.NoIndex).HasColumnName("NoIndex");
        });
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => x.Slug).IsUnique();

        b.HasOne(x => x.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("Tags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class ContentTagConfiguration : IEntityTypeConfiguration<ContentTag>
{
    public void Configure(EntityTypeBuilder<ContentTag> b)
    {
        b.ToTable("ContentTags");
        b.HasKey(x => new { x.ContentId, x.TagId });
        b.HasOne(x => x.Content).WithMany(c => c.ContentTags).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Tag).WithMany(t => t.ContentTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
