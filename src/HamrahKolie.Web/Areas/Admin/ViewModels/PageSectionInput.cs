using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Web.Areas.Admin.ViewModels;

/// <summary>ورودی فرم ویرایش یک سکشن صفحه‌ساز.</summary>
public class PageSectionInput
{
    public long Id { get; set; }
    public SectionType Type { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Body { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonUrl { get; set; }
    public long? ImageId { get; set; }
    public SectionBackground Background { get; set; }
    public SectionPadding Padding { get; set; }
    public bool ShowOnMobile { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPublished { get; set; }

    /// <summary>JSON آیتم‌ها (برای Stats/FeatureCards/Steps) که توسط ادیتور جزیره‌ای پر می‌شود.</summary>
    public string? SettingsJson { get; set; }

    /// <summary>تعداد آیتم‌ها برای سکشن «آخرین مطالب».</summary>
    public int Count { get; set; } = 3;
}
