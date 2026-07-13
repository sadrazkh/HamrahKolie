using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HamrahKolie.Infrastructure.Persistence;

/// <summary>
/// کارخانه زمان‌طراحی (Design-Time) برای ابزار EF Core.
/// وجود این کلاس باعث می‌شود «dotnet ef» به‌جای اجرای کل برنامه، مستقیماً DbContext بسازد.
/// تنظیمات اتصال از فایل‌های appsettings پروژه وب خوانده می‌شود و متغیرهای محیطی می‌توانند آن‌ها را بازنویسی کنند.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(FindWebProjectDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        var provider = configuration["Database:Provider"] ?? "PostgreSql";

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

    private static string FindWebProjectDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            currentDirectory,
            Path.Combine(currentDirectory, "src", "HamrahKolie.Web"),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "HamrahKolie.Web")),
        };

        var directory = candidates.FirstOrDefault(candidate =>
            File.Exists(Path.Combine(candidate, "appsettings.json")));

        return directory
            ?? throw new DirectoryNotFoundException("Could not locate the HamrahKolie.Web configuration directory.");
    }
}
