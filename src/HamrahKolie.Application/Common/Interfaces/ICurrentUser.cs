namespace HamrahKolie.Application.Common.Interfaces;

/// <summary>دسترسی به اطلاعات کاربر جاری درخواست.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }

    /// <summary>آیا کاربر جاری دسترسی مشخص‌شده را دارد؟</summary>
    bool HasPermission(string permissionKey);
}

/// <summary>منبع زمان قابل تست (به‌جای DateTime.UtcNow مستقیم).</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
