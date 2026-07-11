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
