using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>
/// یک بلاک (سکشن) در صفحه‌سازِ سکشن‌محور. سکشن‌ها به یک «کلید صفحه» تعلق دارند
/// (مثلاً «home») و به ترتیب SortOrder رندر می‌شوند.
/// نمایش عمومی: IsEnabled و IsPublished هر دو true باشند. پیش‌نمایش مدیریت: فقط IsEnabled.
/// </summary>
public class PageSection : BaseEntity
{
    /// <summary>کلید صفحه‌ای که این سکشن به آن تعلق دارد (مثل «home»).</summary>
    public string PageKey { get; set; } = "home";

    public SectionType Type { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>منتشرشده؟ (پیش‌نویس تا زمان انتشار برای عموم نمایش داده نمی‌شود)</summary>
    public bool IsPublished { get; set; }

    // ── محتوای عمومی سکشن ────────────────────────────────────────
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Body { get; set; }            // HTML (برای RichText)

    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonUrl { get; set; }

    public long? ImageId { get; set; }
    public MediaFile? Image { get; set; }

    // ── ظاهر ─────────────────────────────────────────────────────
    public SectionBackground Background { get; set; } = SectionBackground.Default;
    public SectionPadding Padding { get; set; } = SectionPadding.Normal;

    public bool ShowOnMobile { get; set; } = true;
    public bool ShowOnDesktop { get; set; } = true;

    /// <summary>تنظیمات مخصوص نوع سکشن به‌صورت JSON (مثل آیتم‌های آمار یا کارت‌ها).</summary>
    public string? SettingsJson { get; set; }
}
