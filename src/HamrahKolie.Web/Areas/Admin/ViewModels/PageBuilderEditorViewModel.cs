using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Web.Areas.Admin.ViewModels;

public sealed record PageBuilderPageOption(string Key, string Title, string PublicUrl, string Kind);

public sealed class PageBuilderEditorViewModel
{
    public string PageKey { get; set; } = "home";
    public string PageTitle { get; set; } = "صفحه اصلی";
    public string PublicUrl { get; set; } = "/";
    public long? SelectedSectionId { get; set; }
    public IReadOnlyList<PageSection> Sections { get; set; } = Array.Empty<PageSection>();
    public IReadOnlyList<PageBuilderPageOption> Pages { get; set; } = Array.Empty<PageBuilderPageOption>();
    public IReadOnlyList<MediaFile> Media { get; set; } = Array.Empty<MediaFile>();
}

public sealed class PageBuilderCanvasViewModel
{
    public string PageKey { get; set; } = "home";
    public string PageTitle { get; set; } = "پیش‌نمایش صفحه";
    public IReadOnlyList<PageSection> Sections { get; set; } = Array.Empty<PageSection>();
}
