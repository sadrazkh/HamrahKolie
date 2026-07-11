using HamrahKolie.Domain.Common;

namespace HamrahKolie.Domain.Entities;

/// <summary>یک فایل رسانه در کتابخانه رسانه (تصویر، سند، ...).</summary>
public class MediaFile : BaseEntity
{
    /// <summary>نام نمایشی فایل.</summary>
    public string FileName { get; set; } = default!;

    /// <summary>مسیر ذخیره نسبی روی Storage (مثل «uploads/2026/07/xxx.webp»).</summary>
    public string StoredPath { get; set; } = default!;

    /// <summary>نشانی عمومی قابل استفاده در سایت.</summary>
    public string Url { get; set; } = default!;

    /// <summary>نوع MIME.</summary>
    public string ContentType { get; set; } = default!;

    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    /// <summary>متن جایگزین (Alt) برای دسترس‌پذیری و سئو.</summary>
    public string? Alt { get; set; }
    public string? Caption { get; set; }

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
