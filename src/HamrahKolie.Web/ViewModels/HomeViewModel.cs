using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Web.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<Content> LatestNews { get; set; } = Array.Empty<Content>();
    public IReadOnlyList<Content> LatestArticles { get; set; } = Array.Empty<Content>();

    /// <summary>سکشن‌های صفحه‌ساز برای صفحه اصلی. اگر خالی باشد، طرح ثابت پیش‌فرض نمایش داده می‌شود.</summary>
    public IReadOnlyList<PageSection> Sections { get; set; } = Array.Empty<PageSection>();
}
