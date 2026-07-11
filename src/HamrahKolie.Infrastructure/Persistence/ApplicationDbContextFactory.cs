using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HamrahKolie.Infrastructure.Persistence;

/// <summary>
/// کارخانه زمان‌طراحی (Design-Time) برای ابزار EF Core.
/// وجود این کلاس باعث می‌شود «dotnet ef» به‌جای اجرای کل برنامه، مستقیماً DbContext بسازد.
/// رشته اتصال از متغیر محیطی خوانده می‌شود و در نبود آن مقدار پیش‌فرض توسعه استفاده می‌شود
/// (فقط برای ساخت Migration؛ نیازی به اتصال واقعی نیست).
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=hamrahkolie;Username=postgres;Password=postgres";

        var provider = Environment.GetEnvironmentVariable("Database__Provider") ?? "PostgreSql";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        }
        else
        {
            options.UseNpgsql(connectionString, npg =>
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        }

        return new ApplicationDbContext(options.Options);
    }
}
