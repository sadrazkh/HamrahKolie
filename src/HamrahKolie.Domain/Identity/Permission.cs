using HamrahKolie.Domain.Common;

namespace HamrahKolie.Domain.Identity;

/// <summary>
/// یک دسترسی (Permission) در سامانه؛ مثل «content.create» یا «donation.refund».
/// دسترسی‌ها ثابت و از پیش تعریف‌شده‌اند و در هنگام راه‌اندازی Seed می‌شوند.
/// </summary>
public class Permission
{
    public long Id { get; set; }

    /// <summary>کلید یکتای دسترسی (مثل «content.create»).</summary>
    public string Key { get; set; } = default!;

    /// <summary>عنوان فارسی برای نمایش در پنل.</summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>گروه/ماژول دسترسی (مثل «محتوا»، «مالی»).</summary>
    public string Group { get; set; } = default!;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// رابطه چند-به-چند بین نقش و دسترسی.
/// </summary>
public class RolePermission
{
    public string RoleId { get; set; } = default!;
    public ApplicationRole Role { get; set; } = default!;

    public long PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}
