using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.SupportRequests;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.SupportRequestView)]
public class SupportRequestsController : Controller
{
    private const int PageSize = 25;
    private readonly ISupportRequestService _service;
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public SupportRequestsController(ISupportRequestService service, ApplicationDbContext db, IAuditService audit)
    {
        _service = service;
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(SupportRequestStatus? status, RequestPriority? priority, string? search, int page = 1)
    {
        ViewData["Title"] = "درخواست‌های حمایت";
        ViewData["Status"] = status;
        ViewData["Priority"] = priority;
        ViewData["Search"] = search;
        ViewData["OpenCount"] = await _service.GetOpenCountAsync();
        var result = await _service.GetAdminListAsync(status, priority, null, search, page, PageSize);
        return View(result);
    }

    public async Task<IActionResult> Detail(long id)
    {
        var request = await _service.GetAdminDetailAsync(id);
        if (request is null) return NotFound();
        ViewData["Title"] = $"درخواست {request.TrackingCode}";
        ViewData["Duplicates"] = await _service.FindPossibleDuplicatesAsync(id);
        await PopulateAssigneesAsync(request.AssignedToUserId);
        return View(request);
    }

    [HttpPost]
    [HasPermission(Permissions.SupportRequestChangeStatus)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(long id, SupportRequestStatus status, string? note)
    {
        var ok = await _service.ChangeStatusAsync(id, status, note);
        if (!ok) return NotFound();
        await _audit.LogAsync("SupportRequest.ChangeStatus", $"وضعیت به «{status}» تغییر کرد.", "SupportRequest", id.ToString());
        TempData["Success"] = "وضعیت درخواست به‌روزرسانی شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.SupportRequestAssign)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(long id, string? userId)
    {
        var ok = await _service.AssignAsync(id, userId);
        if (!ok) return NotFound();
        await _audit.LogAsync("SupportRequest.Assign", "درخواست ارجاع شد.", "SupportRequest", id.ToString());
        TempData["Success"] = "ارجاع درخواست انجام شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.SupportRequestChangeStatus)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPriority(long id, RequestPriority priority)
    {
        await _service.SetPriorityAsync(id, priority);
        TempData["Success"] = "اولویت به‌روزرسانی شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(long id, string body, MessageVisibility visibility)
    {
        var ok = await _service.AddNoteAsync(id, body, visibility);
        if (!ok) { TempData["Error"] = "متن پیام خالی است."; return RedirectToAction(nameof(Detail), new { id }); }
        TempData["Success"] = visibility == MessageVisibility.Applicant ? "پیام برای متقاضی ثبت شد." : "یادداشت داخلی ثبت شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task PopulateAssigneesAsync(string? current)
    {
        ViewBag.Assignees = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Email)
            .Select(u => new SelectListItem(
                (u.FirstName ?? "") + " " + (u.LastName ?? "") + " (" + u.Email + ")",
                u.Id,
                u.Id == current))
            .ToListAsync();
    }
}
