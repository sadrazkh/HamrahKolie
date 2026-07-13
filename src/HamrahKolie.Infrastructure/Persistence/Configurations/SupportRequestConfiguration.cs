using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
{
    public void Configure(EntityTypeBuilder<SupportRequest> b)
    {
        b.ToTable("SupportRequests");
        b.HasKey(x => x.Id);
        b.Property(x => x.TrackingCode).HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Priority).HasConversion<int>();
        b.Property(x => x.DialysisType).HasConversion<int>();
        b.Property(x => x.ApplicantName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.NationalId).HasMaxLength(10);
        b.Property(x => x.Province).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Village).HasMaxLength(150);
        b.Property(x => x.TreatmentCenter).HasMaxLength(200);
        b.Property(x => x.ReferredBy).HasMaxLength(200);
        b.Property(x => x.NeedType).HasMaxLength(150);
        b.Property(x => x.InsuranceStatus).HasMaxLength(150);
        b.Property(x => x.Description).HasColumnType("text");
        b.Property(x => x.EstimatedCost).HasPrecision(18, 0);
        b.Property(x => x.AssignedToUserId).HasMaxLength(450);
        b.Property(x => x.Tags).HasMaxLength(500);
        b.Property(x => x.ConsentVersion).HasMaxLength(32);

        b.HasIndex(x => x.TrackingCode).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.Mobile);
        b.HasIndex(x => x.AssignedToUserId);
    }
}

public class SupportRequestDocumentConfiguration : IEntityTypeConfiguration<SupportRequestDocument>
{
    public void Configure(EntityTypeBuilder<SupportRequestDocument> b)
    {
        b.ToTable("SupportRequestDocuments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.HasOne(x => x.SupportRequest).WithMany(r => r.Documents).HasForeignKey(x => x.SupportRequestId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.MediaFile).WithMany().HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SupportRequestStatusHistoryConfiguration : IEntityTypeConfiguration<SupportRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<SupportRequestStatusHistory> b)
    {
        b.ToTable("SupportRequestStatusHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.FromStatus).HasConversion<int>();
        b.Property(x => x.ToStatus).HasConversion<int>();
        b.Property(x => x.Note).HasMaxLength(1000);
        b.Property(x => x.ChangedByUserId).HasMaxLength(450);
        b.Property(x => x.ChangedByName).HasMaxLength(200);
        b.HasOne(x => x.SupportRequest).WithMany(r => r.History).HasForeignKey(x => x.SupportRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupportRequestMessageConfiguration : IEntityTypeConfiguration<SupportRequestMessage>
{
    public void Configure(EntityTypeBuilder<SupportRequestMessage> b)
    {
        b.ToTable("SupportRequestMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Visibility).HasConversion<int>();
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.AuthorUserId).HasMaxLength(450);
        b.Property(x => x.AuthorName).HasMaxLength(200);
        b.HasOne(x => x.SupportRequest).WithMany(r => r.Messages).HasForeignKey(x => x.SupportRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}
