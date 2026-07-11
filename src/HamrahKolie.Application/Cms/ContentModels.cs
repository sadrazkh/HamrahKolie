using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Cms;

/// <summary>یک ردیف در فهرست مدیریت محتوا.</summary>
public record ContentListItemDto(
    long Id,
    ContentType Type,
    string Title,
    string Slug,
    ContentStatus Status,
    string? CategoryName,
    DateTime? PublishedAt,
    DateTime CreatedAt);

/// <summary>ورودی ایجاد/ویرایش محتوا (از فرم پنل).</summary>
public class ContentEditInput
{
    public ContentType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public long? CategoryId { get; set; }
    public long? FeaturedImageId { get; set; }
    public string Language { get; set; } = "fa";
    public string? Tags { get; set; }              // برچسب‌ها با کاما جدا می‌شوند
    public string? MedicalReviewer { get; set; }

    // سئو
    public string? SeoTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgImageUrl { get; set; }
    public bool NoIndex { get; set; }
}

/// <summary>داده کامل یک محتوا برای صفحه ویرایش.</summary>
public class ContentEditDto : ContentEditInput
{
    public long Id { get; set; }
    public string? FeaturedImageUrl { get; set; }
}
