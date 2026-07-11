using Microsoft.AspNetCore.Identity;

namespace HamrahKolie.Domain.Identity;

/// <summary>
/// نقش سامانه. دسترسی‌ها بر پایه Permission تعریف می‌شوند و به نقش‌ها اختصاص می‌یابند.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    /// <summary>عنوان فارسی نقش برای نمایش در پنل.</summary>
    public string? DisplayName { get; set; }

    /// <summary>توضیح نقش.</summary>
    public string? Description { get; set; }

    /// <summary>نقش‌های سیستمی قابل حذف نیستند (مثل Super Admin).</summary>
    public bool IsSystemRole { get; set; }

    /// <summary>دسترسی‌های اختصاص‌یافته به این نقش.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
