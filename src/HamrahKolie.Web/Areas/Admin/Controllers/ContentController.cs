using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Cms;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using HamrahKolie.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.ContentView)]
public class ContentController : Controller
{
    private const int PageSize = 20;
    private readonly IContentService _content;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public ContentController(IContentService content, ApplicationDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _content = content;
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index(ContentType? type, string? search, ContentStatus? status, int page = 1)
    {
        ViewData["Title"] = "مدیریت محتوا";
        var result = await _content.GetAdminListAsync(type, search, status, page, PageSize);
        ViewData["Type"] = type;
        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(result);
    }

    [HttpGet]
    [HasPermission(Permissions.ContentCreate)]
    public async Task<IActionResult> Create(ContentType type = ContentType.Article)
    {
        ViewData["Title"] = "محتوای جدید";
        await PopulateCategoriesAsync();
        return View("Edit", new ContentEditDto { Type = type, Status = ContentStatus.Draft });
    }

    [HttpPost]
    [HasPermission(Permissions.ContentCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContentEditInput input)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            return View("Edit", ToDto(input, 0));
        }
        var id = await _content.CreateAsync(input, _currentUser.UserId);
        await _audit.LogAsync("Content.Create", $"محتوای «{input.Title}» ایجاد شد.", "Content", id.ToString());
        TempData["Success"] = "محتوا با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    [HasPermission(Permissions.ContentEdit)]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _content.GetForEditAsync(id);
        if (dto is null) return NotFound();
        ViewData["Title"] = "ویرایش محتوا";
        await PopulateCategoriesAsync();
        return View(dto);
    }

    [HttpPost]
    [HasPermission(Permissions.ContentEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ContentEditInput input)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            return View(ToDto(input, id));
        }
        var ok = await _content.UpdateAsync(id, input);
        if (!ok) return NotFound();
        await _audit.LogAsync("Content.Update", $"محتوای «{input.Title}» ویرایش شد.", "Content", id.ToString());
        TempData["Success"] = "تغییرات ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.ContentPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(long id, ContentStatus status)
    {
        var ok = await _content.SetStatusAsync(id, status);
        if (!ok) return NotFound();
        await _audit.LogAsync("Content.SetStatus", $"وضعیت محتوا به «{status}» تغییر کرد.", "Content", id.ToString());
        TempData["Success"] = "وضعیت محتوا به‌روزرسانی شد.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.ContentDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var ok = await _content.SoftDeleteAsync(id);
        if (!ok) return NotFound();
        await _audit.LogAsync("Content.Delete", "محتوا حذف شد.", "Content", id.ToString());
        TempData["Success"] = "محتوا به سطل زباله منتقل شد.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync()
    {
        ViewBag.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();

        ViewBag.Media = await _db.MediaFiles.AsNoTracking()
            .Where(m => m.ContentType.StartsWith("image/"))
            .OrderByDescending(m => m.CreatedAt).Take(100)
            .Select(m => new SelectListItem(m.FileName, m.Id.ToString())).ToListAsync();
    }

    private static ContentEditDto ToDto(ContentEditInput i, long id) => new()
    {
        Id = id, Type = i.Type, Title = i.Title, Slug = i.Slug, Summary = i.Summary, Body = i.Body,
        Status = i.Status, PublishedAt = i.PublishedAt, CategoryId = i.CategoryId,
        FeaturedImageId = i.FeaturedImageId, Language = i.Language, Tags = i.Tags,
        MedicalReviewer = i.MedicalReviewer, SeoTitle = i.SeoTitle, MetaDescription = i.MetaDescription,
        CanonicalUrl = i.CanonicalUrl, OgImageUrl = i.OgImageUrl, NoIndex = i.NoIndex
    };
}
