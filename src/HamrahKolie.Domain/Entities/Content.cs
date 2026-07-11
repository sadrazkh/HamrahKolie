using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>
/// یک واحد محتوا (صفحه، خبر، مقاله، داستان بیمار). مدل یکپارچه برای مدیریت ساده‌تر.
/// اطلاعات سئو به‌صورت Owned Type در همین جدول نگه‌داری می‌شود.
/// </summary>
public class Content : BaseEntity
{
    public ContentType Type { get; set; }

    public string Title { get; set; } = default!;

    /// <summary>نامک یکتا (در هر نوع محتوا).</summary>
    public string Slug { get; set; } = default!;

    /// <summary>خلاصه کوتاه برای فهرست‌ها و اشتراک‌گذاری.</summary>
    public string? Summary { get; set; }

    /// <summary>محتوای کامل (HTML پاک‌سازی‌شده).</summary>
    public string? Body { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    /// <summary>زمان انتشار (UTC). برای محتوای زمان‌بندی‌شده در آینده است.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>شناسه کاربر نویسنده.</summary>
    public string? AuthorId { get; set; }

    /// <summary>تصویر شاخص.</summary>
    public long? FeaturedImageId { get; set; }
    public MediaFile? FeaturedImage { get; set; }

    /// <summary>دسته‌بندی (برای خبر/مقاله).</summary>
    public long? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>زبان محتوا (fa/en).</summary>
    public string Language { get; set; } = "fa";

    /// <summary>اطلاعات سئو.</summary>
    public SeoMetadata Seo { get; set; } = new();

    // اطلاعات بازبینی پزشکی (برای محتوای آموزشی)
    public string? MedicalReviewer { get; set; }
    public DateTime? LastReviewedAt { get; set; }

    public ICollection<ContentTag> ContentTags { get; set; } = new List<ContentTag>();

    /// <summary>آیا این محتوا هم‌اکنون برای عموم قابل نمایش است؟</summary>
    public bool IsPubliclyVisible(DateTime utcNow) =>
        Status == ContentStatus.Published && (PublishedAt == null || PublishedAt <= utcNow);
}

/// <summary>اطلاعات سئوی یک محتوا (Owned Type).</summary>
public class SeoMetadata
{
    public string? SeoTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgImageUrl { get; set; }
    /// <summary>اگر true باشد، موتور جست‌وجو این صفحه را ایندکس نکند.</summary>
    public bool NoIndex { get; set; }
}
