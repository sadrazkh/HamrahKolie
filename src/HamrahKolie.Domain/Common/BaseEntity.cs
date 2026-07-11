namespace HamrahKolie.Domain.Common;

/// <summary>
/// موجودیت پایه با شناسه و فیلدهای ردیابی (Audit) و حذف نرم (Soft Delete).
/// همه Entityهای مهم پروژه از این کلاس ارث می‌برند.
/// </summary>
public abstract class BaseEntity : ISoftDeletable, IAuditable
{
    /// <summary>شناسه یکتا.</summary>
    public long Id { get; set; }

    /// <summary>تاریخ ایجاد (به وقت UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>شناسه کاربر ایجادکننده.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>تاریخ آخرین ویرایش (به وقت UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>شناسه کاربر ویرایش‌کننده.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>آیا رکورد به صورت نرم حذف شده است؟</summary>
    public bool IsDeleted { get; set; }

    /// <summary>تاریخ حذف نرم (به وقت UTC).</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>شناسه کاربر حذف‌کننده.</summary>
    public string? DeletedBy { get; set; }

    /// <summary>توکن همزمانی برای جلوگیری از بازنویسی هم‌زمان.</summary>
    public uint RowVersion { get; set; }
}
