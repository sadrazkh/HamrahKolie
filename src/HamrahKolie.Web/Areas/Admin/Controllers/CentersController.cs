using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Centers;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.CenterView)]
public class CentersController : Controller
{
    private const int PageSize = 25;
    private readonly ICenterService _centers;
    private readonly IAuditService _audit;

    public CentersController(ICenterService centers, IAuditService audit)
    {
        _centers = centers;
        _audit = audit;
    }

    public async Task<IActionResult> Index(bool? approved, string? search, int page = 1)
    {
        ViewData["Title"] = "مراکز دیالیز";
        ViewData["Approved"] = approved;
        ViewData["PendingCount"] = await _centers.GetPendingCountAsync();
        var result = await _centers.GetAdminListAsync(approved, search, page, PageSize);
        return View(result);
    }

    [HttpGet]
    [HasPermission(Permissions.CenterManage)]
    public IActionResult Create()
    {
        ViewData["Title"] = "مرکز جدید";
        return View("Edit", new CenterInput());
    }

    [HttpPost]
    [HasPermission(Permissions.CenterManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CenterInput input)
    {
        if (!ModelState.IsValid) return View("Edit", input);
        var id = await _centers.CreateAsync(input, approved: true);
        await _audit.LogAsync("Center.Create", $"مرکز «{input.Name}» ایجاد شد.", "Center", id.ToString());
        TempData["Success"] = "مرکز ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    [HasPermission(Permissions.CenterManage)]
    public async Task<IActionResult> Edit(long id)
    {
        var c = await _centers.GetAsync(id);
        if (c is null) return NotFound();
        ViewData["Title"] = "ویرایش مرکز";
        return View(ToInput(c));
    }

    [HttpPost]
    [HasPermission(Permissions.CenterManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CenterInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var ok = await _centers.UpdateAsync(input);
        if (!ok) return NotFound();
        TempData["Success"] = "تغییرات ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id = input.Id });
    }

    [HttpPost]
    [HasPermission(Permissions.CenterApprove)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetApproval(long id, bool approved)
    {
        var ok = await _centers.SetApprovalAsync(id, approved);
        if (!ok) return NotFound();
        await _audit.LogAsync("Center.SetApproval", approved ? "مرکز تأیید شد." : "تأیید مرکز لغو شد.", "Center", id.ToString());
        TempData["Success"] = approved ? "مرکز تأیید و منتشر شد." : "تأیید مرکز لغو شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [HasPermission(Permissions.CenterManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _centers.DeleteAsync(id);
        TempData["Success"] = "مرکز حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    private static CenterInput ToInput(Domain.Entities.DialysisCenter c) => new()
    {
        Id = c.Id, Name = c.Name, Type = c.Type, Province = c.Province, City = c.City, Address = c.Address,
        Latitude = c.Latitude, Longitude = c.Longitude, Phone = c.Phone, WorkingHours = c.WorkingHours,
        Services = c.Services, Facilities = c.Facilities, DialysisTypes = c.DialysisTypes,
        AccessibilityNotes = c.AccessibilityNotes, Website = c.Website,
    };
}
