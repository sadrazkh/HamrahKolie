using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Cms;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Infrastructure.Seed;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using HamrahKolie.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.PageBuilderManage)]
public class PageBuilderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPageBuilderService _pageBuilder;
    private readonly IContentService _content;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IAuditService _audit;

    public PageBuilderController(
        ApplicationDbContext db,
        IPageBuilderService pageBuilder,
        IContentService content,
        IHtmlSanitizerService sanitizer,
        IAuditService audit)
    {
        _db = db;
        _pageBuilder = pageBuilder;
        _content = content;
        _sanitizer = sanitizer;
        _audit = audit;
    }

    /// <summary>ویرایشگر بصری چندصفحه‌ای.</summary>
    public async Task<IActionResult> Index(string pageKey = "home", long? selected = null)
    {
        pageKey = NormalizePageKey(pageKey);
        var pages = await BuildPageOptionsAsync();
        var current = pages.FirstOrDefault(x => x.Key == pageKey)
            ?? new PageBuilderPageOption(pageKey, PageTitleFromKey(pageKey), PublicUrl(pageKey), "سفارشی");

        var sections = await _db.PageSections.AsNoTracking()
            .Include(s => s.Image)
            .Where(s => s.PageKey == pageKey)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        if (pageKey == "home" && !sections.Any(s => s.UsesBuilderOrder()))
        {
            // داده‌های قدیمی صفحهٔ اصلی با ترتیب بصری قالب یکی نبودند؛ تا قبل از نخستین
            // جابه‌جایی، لایه‌ها را دقیقاً به همان ترتیبی نشان می‌دهیم که روی بوم دیده می‌شوند.
            sections = sections
                .OrderBy(s => LegacyHomeVisualRank(s.Type))
                .ThenBy(s => s.SortOrder)
                .ToList();
        }
        var media = await _db.MediaFiles.AsNoTracking()
            .Where(m => m.ContentType.StartsWith("image/") || m.ContentType.StartsWith("video/"))
            .OrderByDescending(m => m.CreatedAt)
            .Take(150)
            .ToListAsync();

        ViewData["Title"] = $"ویرایش بصری — {current.Title}";
        return View(new PageBuilderEditorViewModel
        {
            PageKey = pageKey,
            PageTitle = current.Title,
            PublicUrl = current.PublicUrl,
            SelectedSectionId = selected,
            Sections = sections,
            Pages = pages,
            Media = media,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SectionType type, string pageKey = "home", long? beforeId = null)
    {
        pageKey = NormalizePageKey(pageKey);
        if (!Enum.IsDefined(type)) return BadRequest();

        var sections = await _db.PageSections
            .Where(s => s.PageKey == pageKey)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        PrepareHomeLayoutMutation(pageKey, sections);
        var insertOrder = sections.Count + 1;
        if (beforeId.HasValue)
        {
            var before = sections.FirstOrDefault(x => x.Id == beforeId.Value);
            if (before is not null)
            {
                insertOrder = before.SortOrder;
                foreach (var item in sections.Where(x => x.SortOrder >= insertOrder)) item.SortOrder++;
            }
        }

        var section = new PageSection
        {
            PageKey = pageKey,
            Type = type,
            SortOrder = insertOrder,
            IsEnabled = true,
            IsPublished = false,
            ShowOnDesktop = true,
            ShowOnMobile = true,
            Title = DefaultTitle(type),
            SettingsJson = DefaultSettings(type),
        };
        _db.PageSections.Add(section);
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(pageKey);

        if (IsVisualRequest()) return Json(new { ok = true, id = section.Id });
        return RedirectToAction(nameof(Index), new { pageKey, selected = section.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var section = await _db.PageSections.FirstOrDefaultAsync(x => x.Id == id);
        if (section is null) return NotFound();
        ViewData["Title"] = "ویرایش سکشن";
        await PopulateMediaAsync();
        return View(ToInput(section));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PageSectionInput input)
    {
        var section = await _db.PageSections.FirstOrDefaultAsync(x => x.Id == input.Id);
        if (section is null) return NotFound();
        if (NormalizePageKey(input.PageKey) != section.PageKey) return BadRequest();

        ApplyInput(section, input);
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(section.PageKey);
        await _audit.LogAsync("PageBuilder.Edit", $"سکشن «{section.Type}» در صفحه «{section.PageKey}» ویرایش شد.", "PageSection", section.Id.ToString());

        if (IsVisualRequest()) return Json(new { ok = true, id = section.Id, title = section.Title });
        TempData["Success"] = "سکشن ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id = section.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(long id, string pageKey)
    {
        pageKey = NormalizePageKey(pageKey);
        var allSections = await _db.PageSections
            .Where(x => x.PageKey == pageKey)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        PrepareHomeLayoutMutation(pageKey, allSections);
        var source = allSections.FirstOrDefault(x => x.Id == id);
        if (source is null) return NotFound();

        var later = allSections.Where(x => x.SortOrder > source.SortOrder);
        foreach (var item in later) item.SortOrder++;
        var copy = new PageSection
        {
            PageKey = source.PageKey,
            Type = source.Type,
            SortOrder = source.SortOrder + 1,
            IsEnabled = source.IsEnabled,
            IsPublished = false,
            Title = string.IsNullOrWhiteSpace(source.Title) ? null : $"{source.Title} — کپی",
            Subtitle = source.Subtitle,
            Body = source.Body,
            ButtonText = source.ButtonText,
            ButtonUrl = source.ButtonUrl,
            SecondaryButtonText = source.SecondaryButtonText,
            SecondaryButtonUrl = source.SecondaryButtonUrl,
            ImageId = source.ImageId,
            Background = source.Background,
            Padding = source.Padding,
            ShowOnMobile = source.ShowOnMobile,
            ShowOnDesktop = source.ShowOnDesktop,
            SettingsJson = source.SettingsJson,
        };
        _db.PageSections.Add(copy);
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(pageKey);
        if (IsVisualRequest()) return Json(new { ok = true, id = copy.Id });
        return RedirectToAction(nameof(Index), new { pageKey, selected = copy.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, string pageKey)
    {
        pageKey = NormalizePageKey(pageKey);
        var allSections = await _db.PageSections
            .Where(x => x.PageKey == pageKey)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        PrepareHomeLayoutMutation(pageKey, allSections);
        var section = allSections.FirstOrDefault(x => x.Id == id);
        if (section is null) return NotFound();
        _db.PageSections.Remove(section);
        await _db.SaveChangesAsync();
        await NormalizeSortOrdersAsync(pageKey);
        _pageBuilder.InvalidateCache(pageKey);
        if (IsVisualRequest()) return Json(new { ok = true });
        TempData["Success"] = "سکشن حذف شد.";
        return RedirectToAction(nameof(Index), new { pageKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromBody] ReorderRequest? request)
    {
        if (request is null) return BadRequest(new { ok = false, message = "درخواست ترتیب معتبر نیست." });
        var pageKey = NormalizePageKey(request.PageKey);
        var sections = await _db.PageSections.Where(s => s.PageKey == pageKey).ToListAsync();
        var requestedIds = (request.Ids ?? Array.Empty<long>()).Distinct().ToArray();
        if (requestedIds.Length != sections.Count || requestedIds.Any(id => sections.All(s => s.Id != id)))
            return BadRequest(new { ok = false, message = "ترتیب ارسالی با بخش‌های این صفحه مطابقت ندارد." });

        for (var i = 0; i < requestedIds.Length; i++)
        {
            var section = sections.First(s => s.Id == requestedIds[i]);
            section.SortOrder = i + 1;
            if (pageKey == "home") MarkBuilderOrder(section);
        }
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(pageKey);
        return Ok(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishPage(string pageKey)
    {
        pageKey = NormalizePageKey(pageKey);
        var sections = await _db.PageSections.Where(s => s.PageKey == pageKey && s.IsEnabled).ToListAsync();
        foreach (var section in sections) section.IsPublished = true;
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(pageKey);
        await _audit.LogAsync("PageBuilder.Publish", $"صفحه «{pageKey}» منتشر شد.", "PageSection", pageKey);
        if (IsVisualRequest()) return Json(new { ok = true, count = sections.Count });
        TempData["Success"] = "صفحه منتشر شد.";
        return RedirectToAction(nameof(Index), new { pageKey });
    }

    [HttpGet]
    public async Task<IActionResult> Preview(string pageKey = "home") => await Canvas(pageKey);

    /// <summary>خروجی زنده‌ای که داخل iframe ویرایشگر نمایش داده می‌شود.</summary>
    [HttpGet]
    public async Task<IActionResult> Canvas(string pageKey = "home")
    {
        pageKey = NormalizePageKey(pageKey);
        var sections = await _pageBuilder.GetEnabledForPreviewAsync(pageKey);
        ViewData["PageBuilderCanvas"] = true;
        ViewData["Robots"] = "noindex,nofollow";

        if (pageKey == "home")
        {
            var vm = new HomeViewModel
            {
                Sections = sections,
                HasPageBuilderContent = await _pageBuilder.HasSectionsAsync(pageKey),
            };
            try
            {
                vm.LatestNews = await _content.GetLatestPublishedAsync(ContentType.News, 3);
                vm.LatestArticles = await _content.GetLatestPublishedAsync(ContentType.Article, 3);
            }
            catch { }
            return View("~/Views/Home/Index.cshtml", vm);
        }

        return View("Canvas", new PageBuilderCanvasViewModel
        {
            PageKey = pageKey,
            PageTitle = PageTitleFromKey(pageKey),
            Sections = sections,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDefaults(string pageKey = "home")
    {
        pageKey = NormalizePageKey(pageKey);
        if (await _db.PageSections.AnyAsync(s => s.PageKey == pageKey))
        {
            if (IsVisualRequest()) return Conflict(new { ok = false, message = "این صفحه از قبل سکشن دارد." });
            TempData["Error"] = "این صفحه از قبل سکشن دارد.";
            return RedirectToAction(nameof(Index), new { pageKey });
        }

        var defaults = PageBuilderDefaults.Build(pageKey);
        _db.PageSections.AddRange(defaults);
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(pageKey);
        if (IsVisualRequest()) return Json(new { ok = true, count = defaults.Count });
        TempData["Success"] = "طرح اولیهٔ صفحه ایجاد شد.";
        return RedirectToAction(nameof(Index), new { pageKey });
    }

    private void ApplyInput(PageSection section, PageSectionInput input)
    {
        section.Title = input.Title?.Trim();
        section.Subtitle = input.Subtitle?.Trim();
        section.Body = section.Type is SectionType.RichText or SectionType.ImageText
            ? _sanitizer.Sanitize(input.Body)
            : input.Body?.Trim();
        section.ButtonText = input.ButtonText?.Trim();
        section.ButtonUrl = SafeUrl(input.ButtonUrl);
        section.SecondaryButtonText = input.SecondaryButtonText?.Trim();
        section.SecondaryButtonUrl = SafeUrl(input.SecondaryButtonUrl);
        section.ImageId = input.ImageId;
        section.Background = input.Background;
        section.Padding = input.Padding;
        section.ShowOnMobile = input.ShowOnMobile;
        section.ShowOnDesktop = input.ShowOnDesktop;
        section.IsEnabled = input.IsEnabled;
        section.IsPublished = input.IsPublished;
        section.SettingsJson = BuildSettings(section.Type, input, section.SettingsJson);
    }

    private static string BuildSettings(SectionType type, PageSectionInput input, string? currentJson)
    {
        var source = !string.IsNullOrWhiteSpace(input.SettingsJson) ? input.SettingsJson : currentJson;
        JsonObject root;
        try { root = JsonNode.Parse(source ?? "{}") as JsonObject ?? new JsonObject(); }
        catch { root = new JsonObject(); }

        if (type == SectionType.LatestContent) root["count"] = Math.Clamp(input.Count, 1, 12);
        root["style"] = JsonSerializer.SerializeToNode(new SectionStyle(
            NormalizeColor(input.BackgroundColor),
            NormalizeColor(input.TextColor),
            NormalizeColor(input.AccentColor),
            Allowed(input.TextAlign, "start", "center", "end"),
            Math.Clamp(input.ContentWidth, 320, 1600),
            Math.Clamp(input.MinHeight, 0, 1200),
            Math.Clamp(input.PaddingTop, 0, 240),
            Math.Clamp(input.PaddingBottom, 0, 240),
            Math.Clamp(input.PaddingInline, 0, 120),
            Math.Clamp(input.MarginTop, -120, 240),
            Math.Clamp(input.MarginBottom, -120, 240),
            Math.Clamp(input.BorderRadius, 0, 120),
            Allowed(input.Shadow, "none", "small", "medium", "large"),
            CleanCssClass(input.CssClass),
            Allowed(input.BackgroundPosition, "center", "top", "bottom", "left", "right"),
            Math.Clamp(input.OverlayOpacity, 0, 90),
            Allowed(input.Animation, "none", "fade", "slide-up", "zoom")));
        return root.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task<IReadOnlyList<PageBuilderPageOption>> BuildPageOptionsAsync()
    {
        var pages = new List<PageBuilderPageOption>
        {
            new("home", "صفحه اصلی", "/", "اصلی"),
            new("about", "درباره ما", "/Home/About", "ثابت"),
            new("services", "خدمات حمایتی", "/Home/Services", "ثابت"),
            new("campaigns", "کمپین‌ها", "/campaigns", "پویا"),
            new("donate", "حمایت مالی", "/donate", "پویا"),
            new("contact", "تماس با ما", "/forms/contact", "فرم"),
        };
        var cmsPages = await _db.Contents.AsNoTracking()
            .Where(x => x.Type == ContentType.Page)
            .OrderBy(x => x.Title)
            .Select(x => new { x.Slug, x.Title })
            .ToListAsync();
        pages.AddRange(cmsPages.Select(x => new PageBuilderPageOption($"page:{x.Slug}", x.Title, $"/p/{x.Slug}", "برگه")));
        return pages;
    }

    private async Task PopulateMediaAsync()
    {
        ViewBag.Media = await _db.MediaFiles.AsNoTracking()
            .Where(m => m.ContentType.StartsWith("image/") || m.ContentType.StartsWith("video/"))
            .OrderByDescending(m => m.CreatedAt)
            .Take(150)
            .Select(m => new SelectListItem(m.FileName, m.Id.ToString()))
            .ToListAsync();
    }

    private static PageSectionInput ToInput(PageSection section)
    {
        var style = section.GetStyle();
        return new PageSectionInput
        {
            Id = section.Id,
            PageKey = section.PageKey,
            Type = section.Type,
            Title = section.Title,
            Subtitle = section.Subtitle,
            Body = section.Body,
            ButtonText = section.ButtonText,
            ButtonUrl = section.ButtonUrl,
            SecondaryButtonText = section.SecondaryButtonText,
            SecondaryButtonUrl = section.SecondaryButtonUrl,
            ImageId = section.ImageId,
            Background = section.Background,
            Padding = section.Padding,
            ShowOnMobile = section.ShowOnMobile,
            ShowOnDesktop = section.ShowOnDesktop,
            IsEnabled = section.IsEnabled,
            IsPublished = section.IsPublished,
            SettingsJson = section.SettingsJson,
            Count = section.GetCount(3),
            BackgroundColor = style.BackgroundColor,
            TextColor = style.TextColor,
            AccentColor = style.AccentColor,
            TextAlign = style.TextAlign,
            ContentWidth = style.ContentWidth,
            MinHeight = style.MinHeight,
            PaddingTop = style.PaddingTop,
            PaddingBottom = style.PaddingBottom,
            PaddingInline = style.PaddingInline,
            MarginTop = style.MarginTop,
            MarginBottom = style.MarginBottom,
            BorderRadius = style.BorderRadius,
            Shadow = style.Shadow,
            CssClass = style.CssClass,
            BackgroundPosition = style.BackgroundPosition,
            OverlayOpacity = style.OverlayOpacity,
            Animation = style.Animation,
        };
    }

    private async Task NormalizeSortOrdersAsync(string pageKey)
    {
        var sections = await _db.PageSections.Where(x => x.PageKey == pageKey).OrderBy(x => x.SortOrder).ToListAsync();
        for (var i = 0; i < sections.Count; i++) sections[i].SortOrder = i + 1;
        await _db.SaveChangesAsync();
    }

    private static void PrepareHomeLayoutMutation(string pageKey, List<PageSection> sections)
    {
        if (pageKey != "home" || sections.Any(s => s.UsesBuilderOrder())) return;

        var visualOrder = sections
            .OrderBy(s => LegacyHomeVisualRank(s.Type))
            .ThenBy(s => s.SortOrder)
            .ToList();
        for (var i = 0; i < visualOrder.Count; i++)
        {
            visualOrder[i].SortOrder = i + 1;
            MarkBuilderOrder(visualOrder[i]);
        }
        sections.Clear();
        sections.AddRange(visualOrder);
    }

    private static int LegacyHomeVisualRank(SectionType type) => type switch
    {
        SectionType.Hero => 10,
        SectionType.Stats => 11,
        SectionType.FeatureCards => 20,
        SectionType.Steps => 30,
        SectionType.CallToAction => 50,
        SectionType.LatestContent => 60,
        _ => 40,
    };

    private static void MarkBuilderOrder(PageSection section)
    {
        JsonObject root;
        try { root = JsonNode.Parse(section.SettingsJson ?? "{}") as JsonObject ?? new JsonObject(); }
        catch { root = new JsonObject(); }
        root["builderOrder"] = true;
        section.SettingsJson = root.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string DefaultTitle(SectionType type) => type switch
    {
        SectionType.Hero => "عنوان بخش قهرمان",
        SectionType.Stats => "آمارها",
        SectionType.FeatureCards => "کارت‌ها",
        SectionType.Steps => "مراحل",
        SectionType.LatestContent => "آخرین مطالب",
        SectionType.RichText => "متن",
        SectionType.CallToAction => "فراخوان",
        SectionType.Heading => "عنوان جدید",
        SectionType.ImageText => "تصویر و متن",
        SectionType.Faq => "پرسش‌های متداول",
        SectionType.Video => "ویدئو",
        _ => null,
    } ?? "";

    private static string DefaultSettings(SectionType type)
    {
        var root = new JsonObject();
        if (type == SectionType.Stats) root["stats"] = new JsonArray();
        if (type is SectionType.FeatureCards or SectionType.Steps or SectionType.Faq) root["cards"] = new JsonArray();
        if (type == SectionType.LatestContent) root["count"] = 3;
        root["style"] = JsonSerializer.SerializeToNode(new SectionStyle());
        return root.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private bool IsVisualRequest() => Request.Headers["X-PageBuilder"] == "1";

    private static string NormalizePageKey(string? value)
    {
        var key = (value ?? "home").Trim().ToLowerInvariant();
        return key.Length is > 0 and <= 96 && Regex.IsMatch(key, "^[a-z0-9:_-]+$") ? key : "home";
    }

    private static string PageTitleFromKey(string pageKey) => pageKey switch
    {
        "home" => "صفحه اصلی",
        "about" => "درباره ما",
        "services" => "خدمات حمایتی",
        "campaigns" => "کمپین‌ها",
        "donate" => "حمایت مالی",
        "contact" => "تماس با ما",
        _ when pageKey.StartsWith("page:") => pageKey[5..],
        _ => pageKey,
    };

    private static string PublicUrl(string pageKey) => pageKey switch
    {
        "home" => "/",
        "about" => "/Home/About",
        "services" => "/Home/Services",
        "campaigns" => "/campaigns",
        "donate" => "/donate",
        "contact" => "/forms/contact",
        _ when pageKey.StartsWith("page:") => $"/p/{pageKey[5..]}",
        _ => "/",
    };

    private static string? SafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        url = url.Trim();
        if (url.StartsWith('/') || url.StartsWith('#') || Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https") return url;
        return null;
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        color = color.Trim();
        return Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$") ? color.ToLowerInvariant() : null;
    }

    private static string? CleanCssClass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = Regex.Replace(value, "[^a-zA-Z0-9_\\- ]", "").Trim();
        return cleaned.Length > 120 ? cleaned[..120] : cleaned;
    }

    private static string Allowed(string? value, params string[] allowed)
        => allowed.Contains(value, StringComparer.OrdinalIgnoreCase) ? value!.ToLowerInvariant() : allowed[0];

    public sealed record ReorderRequest(string PageKey, long[]? Ids);
}
