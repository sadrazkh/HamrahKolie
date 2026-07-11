namespace HamrahKolie.Domain.Common;

/// <summary>موجودیتی که قابلیت حذف نرم دارد.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}

/// <summary>موجودیتی که فیلدهای ردیابی ایجاد و ویرایش دارد.</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
