using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.SupportRequests;
using HamrahKolie.Domain.Identity;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using HamrahKolie.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Controllers;

/// <summary>پورتال مرکز درمانی: ثبت و مدیریت بیماران معرفی‌شده به‌همراه مدارک.</summary>
[Route("hospital")]
[HasPermission(Permissions.HospitalPortal)]
public class HospitalController : Controller
{
    private const int PageSize = 20;
    private const string ConsentVersion = "v1";

    private readonly ISupportRequestService _service;
    private readonly ICurrentUser _currentUser;
    private readonly ApplicationDbContext _db;
    private readonly IFileUploadService _uploads;

    public HospitalController(ISupportRequestService service, ICurrentUser currentUser,
        ApplicationDbContext db, IFileUploadService uploads)
    {
        _service = service;
        _currentUser = currentUser;
        _db = db;
        _uploads = uploads;
    }

    private async Task<(long? centerId, string? centerName)> GetCenterAsync()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return (null, null);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CenterId is null) return (null, null);
        var name = await _db.DialysisCenters.AsNoTracking()
            .Where(c => c.Id == user.CenterId).Select(c => c.Name).FirstOrDefaultAsync();
        return (user.CenterId, name);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var (centerId, centerName) = await GetCenterAsync();
        ViewData["Title"] = "پورتال مرکز درمانی";
        ViewData["CenterName"] = centerName;
        if (centerId is null) return View("NoCenter");

        var result = await _service.GetForCenterAsync(centerId.Value, page, PageSize);
        return View(result);
    }

    [HttpGet("new")]
    public async Task<IActionResult> New()
    {
        var (centerId, centerName) = await GetCenterAsync();
        if (centerId is null) return View("NoCenter");
        ViewData["Title"] = "ثبت بیمار جدید";
        ViewData["CenterName"] = centerName;
        return View(new SupportRequestInput());
    }

    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(SupportRequestInput input, List<IFormFile>? documents)
    {
        var (centerId, centerName) = await GetCenterAsync();
        if (centerId is null) return View("NoCenter");
        ViewData["Title"] = "ثبت بیمار جدید";
        ViewData["CenterName"] = centerName;
        if (!ModelState.IsValid) return View(input);

        input.ReferringCenterId = centerId;
        var (id, tracking) = await _service.SubmitAsync(input, ConsentVersion);

        if (documents is not null)
        {
            foreach (var file in documents.Where(f => f.Length > 0).Take(15))
            {
                var up = await _uploads.SaveAsync(file);
                if (up.Success)
                    await _service.AddDocumentAsync(id, up.Media!.Id, up.Media.FileName, uploadedByApplicant: false);
            }
        }

        TempData["Success"] = $"بیمار با کد پیگیری {tracking} ثبت شد.";
        return RedirectToAction(nameof(Patient), new { id });
    }

    [HttpGet("patient/{id}")]
    public async Task<IActionResult> Patient(long id)
    {
        var (centerId, centerName) = await GetCenterAsync();
        if (centerId is null) return View("NoCenter");

        var request = await _service.GetForCenterDetailAsync(id, centerId.Value);
        if (request is null) return NotFound();

        ViewData["Title"] = $"بیمار {request.TrackingCode}";
        ViewData["CenterName"] = centerName;
        return View(request);
    }

    [HttpPost("patient/{id}/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(long id, string? title, IFormFile? file)
    {
        var (centerId, _) = await GetCenterAsync();
        if (centerId is null) return View("NoCenter");

        var request = await _service.GetForCenterDetailAsync(id, centerId.Value);
        if (request is null) return NotFound();

        var up = await _uploads.SaveAsync(file);
        if (!up.Success) { TempData["Error"] = up.Error; return RedirectToAction(nameof(Patient), new { id }); }

        var docTitle = string.IsNullOrWhiteSpace(title) ? up.Media!.FileName : title.Trim();
        await _service.AddDocumentAsync(id, up.Media!.Id, docTitle, uploadedByApplicant: false);
        TempData["Success"] = "مدرک بارگذاری شد.";
        return RedirectToAction(nameof(Patient), new { id });
    }
}
