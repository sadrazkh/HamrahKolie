using System.Text.Json;
using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Infrastructure.Seed;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.PageBuilderManage)]
public class PageBuilderController : Controller
{
    private const string PageKey = "home";
    private readonly ApplicationDbContext _db;
    private readonly IPageBuilderService _pageBuilder;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IAuditService _audit;

    public PageBuilderController(
        ApplicationDbContext db, IPageBuilderService pageBuilder, IHtmlSanitizerService sanitizer, IAuditService audit)
    {
        _db = db;
        _pageBuilder = pageBuilder;
        _sanitizer = sanitizer;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "صفحه‌ساز صفحه اصلی";
        var sections = await _db.PageSections.AsNoTracking()
            .Where(s => s.PageKey == PageKey)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        return View(sections);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SectionType type)
    {
        var maxOrder = await _db.PageSections
            .Where(s => s.PageKey == PageKey)
            .Select(s => (int?)s.SortOrder).MaxAsync() ?? 0;

        var section = new PageSection
        {
            PageKey = PageKey, Type = type, SortOrder = maxOrder + 1,
            IsEnabled = true, IsPublished = false, Title = DefaultTitle(type),
        };
        _db.PageSections.Add(section);
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(PageKey);
        return RedirectToAction(nameof(Edit), new { id = section.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var s = await _db.PageSections.FirstOrDefaultAsync(x => x.Id == id && x.PageKey == PageKey);
        if (s is null) return NotFound();
        ViewData["Title"] = "ویرایش سکشن";
        await PopulateMediaAsync();
        return View(ToInput(s));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PageSectionInput input)
    {
        var s = await _db.PageSections.FirstOrDefaultAsync(x => x.Id == input.Id && x.PageKey == PageKey);
        if (s is null) return NotFound();

        s.Title = input.Title?.Trim();
        s.Subtitle = input.Subtitle?.Trim();
        s.Body = s.Type == SectionType.RichText ? _sanitizer.Sanitize(input.Body) : null;
        s.ButtonText = input.ButtonText?.Trim();
        s.ButtonUrl = input.ButtonUrl?.Trim();
        s.SecondaryButtonText = input.SecondaryButtonText?.Trim();
        s.SecondaryButtonUrl = input.SecondaryButtonUrl?.Trim();
        s.ImageId = input.ImageId;
        s.Background = input.Background;
        s.Padding = input.Padding;
        s.ShowOnMobile = input.ShowOnMobile;
        s.ShowOnDesktop = input.ShowOnDesktop;
        s.IsEnabled = input.IsEnabled;
        s.IsPublished = input.IsPublished;
        s.SettingsJson = BuildSettings(s.Type, input);

        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(PageKey);
        await _audit.LogAsync("PageBuilder.Edit", $"سکشن «{s.Type}» ویرایش شد.", "PageSection", s.Id.ToString());
        TempData["Success"] = "سکشن ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id = s.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var s = await _db.PageSections.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        _db.PageSections.Remove(s);
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(PageKey);
        TempData["Success"] = "سکشن حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromBody] long[] ids)
    {
        var sections = await _db.PageSections.Where(s => s.PageKey == PageKey).ToListAsync();
        for (var i = 0; i < ids.Length; i++)
        {
            var s = sections.FirstOrDefault(x => x.Id == ids[i]);
            if (s is not null) s.SortOrder = i + 1;
        }
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(PageKey);
        return Ok(new { ok = true });
    }

    [HttpGet]
    public async Task<IActionResult> Preview()
    {
        var sections = await _pageBuilder.GetEnabledForPreviewAsync(PageKey);
        return View(sections);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDefaults()
    {
        if (await _db.PageSections.AnyAsync(s => s.PageKey == PageKey))
        {
            TempData["Error"] = "سکشن‌هایی از قبل وجود دارند. ابتدا آن‌ها را حذف کنید.";
            return RedirectToAction(nameof(Index));
        }
        _db.PageSections.AddRange(DbSeeder.BuildDefaultHomeSections());
        await _db.SaveChangesAsync();
        _pageBuilder.InvalidateCache(PageKey);
        TempData["Success"] = "سکشن‌های پیش‌فرض ایجاد شدند. برای فعال‌سازی روی صفحه اصلی، آن‌ها را منتشر کنید.";
        return RedirectToAction(nameof(Index));
    }

    // ── کمکی ─────────────────────────────────────────────────────
    private async Task PopulateMediaAsync()
    {
        ViewBag.Media = await _db.MediaFiles.AsNoTracking()
            .Where(m => m.ContentType.StartsWith("image/"))
            .OrderByDescending(m => m.CreatedAt).Take(100)
            .Select(m => new SelectListItem(m.FileName, m.Id.ToString())).ToListAsync();
    }

    private static string? BuildSettings(SectionType type, PageSectionInput input)
    {
        switch (type)
        {
            case SectionType.LatestContent:
                return JsonSerializer.Serialize(new { count = Math.Clamp(input.Count, 1, 12) });
            case SectionType.Stats:
            case SectionType.FeatureCards:
            case SectionType.Steps:
                // اعتبارسنجی JSON دریافتی از ادیتور جزیره‌ای.
                if (string.IsNullOrWhiteSpace(input.SettingsJson)) return null;
                try { using var _ = JsonDocument.Parse(input.SettingsJson); return input.SettingsJson; }
                catch { return null; }
            default:
                return null;
        }
    }

    private static PageSectionInput ToInput(PageSection s) => new()
    {
        Id = s.Id, Type = s.Type, Title = s.Title, Subtitle = s.Subtitle, Body = s.Body,
        ButtonText = s.ButtonText, ButtonUrl = s.ButtonUrl,
        SecondaryButtonText = s.SecondaryButtonText, SecondaryButtonUrl = s.SecondaryButtonUrl,
        ImageId = s.ImageId, Background = s.Background, Padding = s.Padding,
        ShowOnMobile = s.ShowOnMobile, ShowOnDesktop = s.ShowOnDesktop,
        IsEnabled = s.IsEnabled, IsPublished = s.IsPublished, SettingsJson = s.SettingsJson,
        Count = s.GetCount(3),
    };

    private static string DefaultTitle(SectionType type) => type switch
    {
        SectionType.Hero => "عنوان بخش قهرمان",
        SectionType.Stats => "آمارها",
        SectionType.FeatureCards => "کارت‌ها",
        SectionType.Steps => "مراحل",
        SectionType.LatestContent => "آخرین مطالب",
        SectionType.RichText => "متن",
        SectionType.CallToAction => "فراخوان",
        _ => "سکشن جدید"
    };
}
