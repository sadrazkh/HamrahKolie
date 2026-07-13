using System.Linq.Expressions;
using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Persistence;

/// <summary>
/// DbContext اصلی سامانه که Identity و موجودیت‌های دامنه را در بر می‌گیرد.
/// فیلتر سراسری حذف نرم روی همه موجودیت‌های <see cref="BaseEntity"/> اعمال می‌شود.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ── محتوا (CMS) ──────────────────────────────────────────────
    public DbSet<Content> Contents => Set<Content>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContentTag> ContentTags => Set<ContentTag>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<PageSection> PageSections => Set<PageSection>();

    // ── کمپین و کمک مالی ─────────────────────────────────────────
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignUpdate> CampaignUpdates => Set<CampaignUpdate>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OfflinePayment> OfflinePayments => Set<OfflinePayment>();

    // ── درخواست حمایت ────────────────────────────────────────────
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<SupportRequestDocument> SupportRequestDocuments => Set<SupportRequestDocument>();
    public DbSet<SupportRequestStatusHistory> SupportRequestStatusHistory => Set<SupportRequestStatusHistory>();
    public DbSet<SupportRequestMessage> SupportRequestMessages => Set<SupportRequestMessage>();

    // ── داوطلبان و مراکز ─────────────────────────────────────────
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<DialysisCenter> DialysisCenters => Set<DialysisCenter>();

    // ── اطلاع‌رسانی و فرم‌ساز ─────────────────────────────────────
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();
    public DbSet<FormField> FormFields => Set<FormField>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // اعمال همه IEntityTypeConfiguration های موجود در این اسمبلی.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // فیلتر سراسری حذف نرم برای هر موجودیتی که از BaseEntity ارث می‌برد.
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(prop), parameter);
                entityType.SetQueryFilter(filter);
            }

            // فیلد RowVersion در نسخه اول به‌عنوان ستون نگاشت نمی‌شود؛ توکن همزمانی
            // اختصاصی هر Provider (مثل xmin در PostgreSQL) در مرحله بعد به موجودیت‌های
            // حساس (مثل پرداخت) افزوده می‌شود.
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType).Ignore(nameof(BaseEntity.RowVersion));
            }
        }
    }
}
