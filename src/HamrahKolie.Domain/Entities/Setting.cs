using HamrahKolie.Domain.Common;

namespace HamrahKolie.Domain.Entities;

/// <summary>
/// یک تنظیم سامانه به‌صورت کلید/مقدار. مقادیر به‌صورت رشته ذخیره می‌شوند و
/// در لایه Application به نوع مناسب تبدیل و Cache می‌شوند.
/// </summary>
public class Setting : BaseEntity
{
    /// <summary>کلید یکتا (مثل «organization.name»).</summary>
    public string Key { get; set; } = default!;

    /// <summary>مقدار (رشته؛ می‌تواند JSON باشد).</summary>
    public string? Value { get; set; }

    /// <summary>گروه تنظیم برای دسته‌بندی در پنل (مثل «مؤسسه»، «پرداخت»).</summary>
    public string Group { get; set; } = "general";

    /// <summary>آیا این تنظیم حساس است؟ (در Audit و نمایش با احتیاط برخورد شود)</summary>
    public bool IsSensitive { get; set; }

    /// <summary>آیا فقط Super Admin مجاز به تغییر است؟</summary>
    public bool IsSuperAdminOnly { get; set; }
}
