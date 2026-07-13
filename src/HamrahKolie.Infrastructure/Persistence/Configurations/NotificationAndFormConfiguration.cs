using HamrahKolie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamrahKolie.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.RecipientUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Message).HasMaxLength(1000);
        b.Property(x => x.Url).HasMaxLength(500);
        b.HasIndex(x => new { x.RecipientUserId, x.IsRead });
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> b)
    {
        b.ToTable("NotificationTemplates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(128).IsRequired();
        b.Property(x => x.Channel).HasConversion<int>();
        b.Property(x => x.Subject).HasMaxLength(300);
        b.Property(x => x.Body).HasColumnType("text").IsRequired();
        b.Property(x => x.Language).HasMaxLength(8);
        b.Property(x => x.AvailableTokens).HasMaxLength(500);
        b.HasIndex(x => new { x.Key, x.Channel }).IsUnique();
    }
}

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> b)
    {
        b.ToTable("NotificationLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Channel).HasConversion<int>();
        b.Property(x => x.Recipient).HasMaxLength(300).IsRequired();
        b.Property(x => x.TemplateKey).HasMaxLength(128);
        b.Property(x => x.Subject).HasMaxLength(300);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Error).HasMaxLength(1000);
        b.HasIndex(x => x.CreatedAt);
    }
}

public class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public void Configure(EntityTypeBuilder<FormDefinition> b)
    {
        b.ToTable("FormDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(280).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.SuccessMessage).HasMaxLength(500);
        b.Property(x => x.SubmitLabel).HasMaxLength(100);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> b)
    {
        b.ToTable("FormFields");
        b.HasKey(x => x.Id);
        b.Property(x => x.Label).HasMaxLength(200).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Placeholder).HasMaxLength(200);
        b.Property(x => x.HelpText).HasMaxLength(300);
        b.Property(x => x.Options).HasColumnType("text");
        b.HasOne(x => x.Form).WithMany(f => f.Fields).HasForeignKey(x => x.FormDefinitionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> b)
    {
        b.ToTable("FormSubmissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.DataJson).HasColumnType("text");
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.ReviewNote).HasMaxLength(1000);
        b.HasOne(x => x.Form).WithMany().HasForeignKey(x => x.FormDefinitionId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.FormDefinitionId, x.IsReviewed });
    }
}
