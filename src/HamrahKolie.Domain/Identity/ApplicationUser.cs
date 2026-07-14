using Microsoft.AspNetCore.Identity;

namespace HamrahKolie.Domain.Identity;

/// <summary>
/// کاربر سامانه. برای کاربران پنل مدیریت و همچنین کاربران عمومی استفاده می‌شود.
/// ورود می‌تواند با ایمیل/رمز یا موبایل/OTP باشد.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>نام.</summary>
    public string? FirstName { get; set; }

    /// <summary>نام خانوادگی.</summary>
    public string? LastName { get; set; }

    /// <summary>تاریخ ایجاد حساب (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>آخرین ورود موفق (UTC).</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>آیا حساب فعال است؟ (غیرفعال‌سازی نرم بدون حذف)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>در صورت حساب «مرکز درمانی»، شناسه مرکز مرتبط.</summary>
    public long? CenterId { get; set; }

    /// <summary>نام کامل نمایشی.</summary>
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
