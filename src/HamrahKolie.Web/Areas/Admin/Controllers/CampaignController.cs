using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.CampaignView)]
public class CampaignController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISlugService _slug;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IAuditService _audit;

    public CampaignController(ApplicationDbContext db, ISlugService slug, IHtmlSanitizerService sanitizer, IAuditService audit)
    {
        _db = db;
        _slug = slug;
        _sanitizer = sanitizer;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "کمپین‌ها";
        var items = await _db.Campaigns.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt).ToListAsync();
        return View(items);
    }

    [HttpGet]
    [HasPermission(Permissions.CampaignManage)]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "کمپین جدید";
        await PopulateMediaAsync();
        return View("Edit", new CampaignInput { Status = CampaignStatus.Draft, ShowExactAmount = true });
    }

    [HttpPost]
    [HasPermission(Permissions.CampaignManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CampaignInput input)
    {
        if (!ModelState.IsValid) { await PopulateMediaAsync(); return View("Edit", input); }

        var c = new Campaign();
        await MapAsync(c, input, isNew: true);
        _db.Campaigns.Add(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Campaign.Create", $"کمپین «{c.Title}» ایجاد شد.", "Campaign", c.Id.ToString());
        TempData["Success"] = "کمپین ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id = c.Id });
    }

    [HttpGet]
    [HasPermission(Permissions.CampaignManage)]
    public async Task<IActionResult> Edit(long id)
    {
        var c = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        ViewData["Title"] = "ویرایش کمپین";
        await PopulateMediaAsync();
        return View(ToInput(c));
    }

    [HttpPost]
    [HasPermission(Permissions.CampaignManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CampaignInput input)
    {
        var c = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == input.Id);
        if (c is null) return NotFound();
        if (!ModelState.IsValid) { await PopulateMediaAsync(); return View(input); }

        await MapAsync(c, input, isNew: false);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Campaign.Update", $"کمپین «{c.Title}» ویرایش شد.", "Campaign", c.Id.ToString());
        TempData["Success"] = "تغییرات ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id = c.Id });
    }

    [HttpPost]
    [HasPermission(Permissions.CampaignPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(long id, CampaignStatus status)
    {
        var c = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        c.Status = status;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Campaign.SetStatus", $"وضعیت کمپین به «{status}» تغییر کرد.", "Campaign", id.ToString());
        TempData["Success"] = "وضعیت کمپین به‌روزرسانی شد.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.CampaignManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var c = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        _db.Campaigns.Remove(c);
        await _db.SaveChangesAsync();
        TempData["Success"] = "کمپین حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    private async Task MapAsync(Campaign c, CampaignInput input, bool isNew)
    {
        c.Title = input.Title.Trim();
        c.ShortDescription = input.ShortDescription?.Trim();
        c.Description = _sanitizer.Sanitize(input.Description);
        c.GoalAmount = decimal.Truncate(input.GoalAmount);
        c.Status = input.Status;
        c.IsUrgent = input.IsUrgent;
        c.ShowExactAmount = input.ShowExactAmount;
        c.Province = input.Province?.Trim();
        c.City = input.City?.Trim();
        c.NeedType = input.NeedType?.Trim();
        c.FeaturedImageId = input.FeaturedImageId;
        c.StartDate = input.StartDate;
        c.EndDate = input.EndDate;
        c.MinDonation = input.MinDonation;
        c.MaxDonation = input.MaxDonation;

        var desired = string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug;
        var baseSlug = _slug.Generate(desired);
        if (isNew || !string.Equals(baseSlug, c.Slug, StringComparison.Ordinal))
        {
            c.Slug = await _slug.GenerateUniqueAsync(baseSlug,
                candidate => _db.Campaigns.AnyAsync(x => x.Slug == candidate && x.Id != c.Id));
        }
    }

    private async Task PopulateMediaAsync()
    {
        ViewBag.Media = await _db.MediaFiles.AsNoTracking()
            .Where(m => m.ContentType.StartsWith("image/"))
            .OrderByDescending(m => m.CreatedAt).Take(100)
            .Select(m => new SelectListItem(m.FileName, m.Id.ToString())).ToListAsync();
    }

    private static CampaignInput ToInput(Campaign c) => new()
    {
        Id = c.Id, Title = c.Title, Slug = c.Slug, ShortDescription = c.ShortDescription,
        Description = c.Description, GoalAmount = c.GoalAmount, Status = c.Status, IsUrgent = c.IsUrgent,
        ShowExactAmount = c.ShowExactAmount, Province = c.Province, City = c.City, NeedType = c.NeedType,
        FeaturedImageId = c.FeaturedImageId, StartDate = c.StartDate, EndDate = c.EndDate,
        MinDonation = c.MinDonation, MaxDonation = c.MaxDonation,
        CollectedAmount = c.CollectedAmount, SupporterCount = c.SupporterCount,
    };
}
