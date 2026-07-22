using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Centers;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Enums;
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
        return View("Edit", new CenterInput
        {
            Features = HospitalFeature.Default,
            SelectedFeatures = HospitalFeatureCatalog.Enabled(HospitalFeature.Default).Select(f => f.Flag).ToList(),
        });
    }

    [HttpPost]
    [HasPermission(Permissions.CenterManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CenterInput input)
    {
        input.Features = HospitalFeatureCatalog.Combine(input.SelectedFeatures);
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
        input.Features = HospitalFeatureCatalog.Combine(input.SelectedFeatures);
        if (!ModelState.IsValid) return View(input);
        var ok = await _centers.UpdateAsync(input);
        if (ok) await _audit.LogAsync("Center.Update", $"مرکز «{input.Name}» ویرایش شد.", "Center", input.Id.ToString());
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
    public async Task<IActionResult> SetFeatures(long id, List<HospitalFeature>? selectedFeatures, int? monthlyQuota,
        bool? approved, string? search, int page = 1)
    {
        var features = HospitalFeatureCatalog.Combine(selectedFeatures);
        var ok = await _centers.SetFeaturesAsync(id, features, monthlyQuota);
        if (!ok) return NotFound();
        await _audit.LogAsync("Center.SetFeatures",
            $"امکانات پورتال مرکز به {HospitalFeatureCatalog.Enabled(features).Count()} مورد تغییر کرد.", "Center", id.ToString());
        TempData["Success"] = "امکانات مرکز به‌روزرسانی شد.";
        return RedirectToAction(nameof(Index), new { approved, search, page });
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
        Features = c.Features, MonthlyPatientQuota = c.MonthlyPatientQuota,
        SelectedFeatures = HospitalFeatureCatalog.Enabled(c.Features).Select(f => f.Flag).ToList(),
    };
}
