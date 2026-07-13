using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Volunteers;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.VolunteerView)]
public class VolunteersController : Controller
{
    private const int PageSize = 25;
    private readonly IVolunteerService _service;
    private readonly IAuditService _audit;

    public VolunteersController(IVolunteerService service, IAuditService audit)
    {
        _service = service;
        _audit = audit;
    }

    public async Task<IActionResult> Index(VolunteerStatus? status, CollaborationType? type, string? search, int page = 1)
    {
        ViewData["Title"] = "داوطلبان";
        ViewData["Status"] = status;
        ViewData["PendingCount"] = await _service.GetPendingCountAsync();
        var result = await _service.GetAdminListAsync(status, type, search, page, PageSize);
        return View(result);
    }

    public async Task<IActionResult> Detail(long id)
    {
        var v = await _service.GetAsync(id);
        if (v is null) return NotFound();
        ViewData["Title"] = v.FullName;
        return View(v);
    }

    [HttpPost]
    [HasPermission(Permissions.VolunteerManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(long id, VolunteerStatus status)
    {
        var ok = await _service.SetStatusAsync(id, status);
        if (!ok) return NotFound();
        await _audit.LogAsync("Volunteer.SetStatus", $"وضعیت داوطلب به «{status}» تغییر کرد.", "Volunteer", id.ToString());
        TempData["Success"] = "وضعیت داوطلب به‌روزرسانی شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.VolunteerManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetNotes(long id, string? notes)
    {
        await _service.SetNotesAsync(id, notes);
        TempData["Success"] = "یادداشت ذخیره شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
