using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> b)
    {
        b.ToTable("Campaigns");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(320).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(1000);
        b.Property(x => x.Description).HasColumnType("text");
        b.Property(x => x.Province).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.NeedType).HasMaxLength(150);
        b.Property(x => x.GoalAmount).HasPrecision(18, 0);
        b.Property(x => x.CollectedAmount).HasPrecision(18, 0);
        b.Property(x => x.MinDonation).HasPrecision(18, 0);
        b.Property(x => x.MaxDonation).HasPrecision(18, 0);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.Status);

        b.HasOne(x => x.FeaturedImage).WithMany().HasForeignKey(x => x.FeaturedImageId).OnDelete(DeleteBehavior.SetNull);

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

public class CampaignUpdateConfiguration : IEntityTypeConfiguration<CampaignUpdate>
{
    public void Configure(EntityTypeBuilder<CampaignUpdate> b)
    {
        b.ToTable("CampaignUpdates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Body).HasColumnType("text");
        b.HasOne(x => x.Campaign).WithMany(c => c.Updates).HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DonorConfiguration : IEntityTypeConfiguration<Donor>
{
    public void Configure(EntityTypeBuilder<Donor> b)
    {
        b.ToTable("Donors");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.TotalDonated).HasPrecision(18, 0);
        b.HasIndex(x => x.Mobile).IsUnique();
    }
}

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> b)
    {
        b.ToTable("Donations");
        b.HasKey(x => x.Id);
        b.Property(x => x.TrackingCode).HasMaxLength(32).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 0);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Method).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.DonorName).HasMaxLength(150).IsRequired();
        b.Property(x => x.DonorMobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.DonorEmail).HasMaxLength(200);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => x.TrackingCode).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);

        b.HasOne(x => x.Donor).WithMany().HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Campaign).WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 0);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Authority).HasMaxLength(128);
        b.Property(x => x.ReferenceId).HasMaxLength(128);
        b.Property(x => x.IdempotencyKey).HasMaxLength(64).IsRequired();
        b.Property(x => x.RawResponse).HasColumnType("text");
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => x.Authority);

        b.HasOne(x => x.Donation).WithOne(d => d.Payment).HasForeignKey<Payment>(x => x.DonationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OfflinePaymentConfiguration : IEntityTypeConfiguration<OfflinePayment>
{
    public void Configure(EntityTypeBuilder<OfflinePayment> b)
    {
        b.ToTable("OfflinePayments");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReferenceNumber).HasMaxLength(100);
        b.Property(x => x.PaidToAccount).HasMaxLength(100);
        b.Property(x => x.ReviewStatus).HasConversion<int>();
        b.Property(x => x.ReviewedBy).HasMaxLength(450);
        b.Property(x => x.ReviewNote).HasMaxLength(500);

        b.HasOne(x => x.Donation).WithOne(d => d.OfflinePayment).HasForeignKey<OfflinePayment>(x => x.DonationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReceiptImage).WithMany().HasForeignKey(x => x.ReceiptImageId).OnDelete(DeleteBehavior.SetNull);
    }
}
