using System.Text;
using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.SupportRequests;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using HamrahKolie.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Controllers;

/// <summary>پورتال مرکز درمانی: ثبت و مدیریت بیماران معرفی‌شده به‌همراه مدارک.
/// امکانات هر مرکز به‌صورت داینامیک توسط مدیر سامانه تعیین می‌شود (<see cref="HospitalFeature"/>).</summary>
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

    private async Task<DialysisCenter?> GetCenterAsync()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return null;
        var centerId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.CenterId).FirstOrDefaultAsync();
        if (centerId is null) return null;
        return await _db.DialysisCenters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == centerId);
    }

    /// <summary>بارگذاری مرکز + قراردادن اطلاعات مشترک در ViewData برای لایه نمایش و منو.</summary>
    private async Task<DialysisCenter?> PrepareAsync()
    {
        var center = await GetCenterAsync();
        if (center is not null)
        {
            ViewData["CenterName"] = center.Name;
            ViewData["Features"] = center.Features;
        }
        return center;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var center = await PrepareAsync();
        ViewData["Title"] = "پورتال مرکز درمانی";
        if (center is null) return View("NoCenter");

        CenterPatientStats? stats = null;
        if (center.Has(HospitalFeature.ViewStatistics))
        {
            stats = await _service.GetCenterStatsAsync(center.Id);
            ViewData["Stats"] = stats;
        }
        ViewData["MonthlyQuota"] = center.MonthlyPatientQuota;
        if (center.MonthlyPatientQuota is not null)
            ViewData["ThisMonthCount"] = stats?.ThisMonth ?? await _service.CountCenterThisMonthAsync(center.Id);

        var result = await _service.GetForCenterAsync(center.Id, page, PageSize);
        return View(result);
    }

    [HttpGet("new")]
    public async Task<IActionResult> New()
    {
        var center = await PrepareAsync();
        if (center is null) return View("NoCenter");
        if (!center.Has(HospitalFeature.PatientRegistration)) return FeatureDisabled();

        ViewData["Title"] = "ثبت بیمار جدید";
        return View(new SupportRequestInput());
    }

    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(SupportRequestInput input, List<IFormFile>? documents)
    {
        var center = await PrepareAsync();
        if (center is null) return View("NoCenter");
        if (!center.Has(HospitalFeature.PatientRegistration)) return FeatureDisabled();
        ViewData["Title"] = "ثبت بیمار جدید";

        if (center.MonthlyPatientQuota is int quota)
        {
            var used = await _service.CountCenterThisMonthAsync(center.Id);
            if (used >= quota)
            {
                ModelState.AddModelError(string.Empty,
                    $"سقف ثبت بیمار این ماه ({quota}) تکمیل شده است. برای افزایش سقف با مدیر سامانه تماس بگیرید.");
                return View(input);
            }
        }

        if (!ModelState.IsValid) return View(input);

        input.ReferringCenterId = center.Id;
        var (id, tracking) = await _service.SubmitAsync(input, ConsentVersion);

        if (center.Has(HospitalFeature.DocumentUpload) && documents is not null)
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
        var center = await PrepareAsync();
        if (center is null) return View("NoCenter");

        var request = await _service.GetForCenterDetailAsync(id, center.Id);
        if (request is null) return NotFound();

        ViewData["Title"] = $"بیمار {request.TrackingCode}";
        return View(request);
    }

    [HttpGet("patient/{id}/edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var center = await PrepareAsync();
        if (center is null) return View("NoCenter");
        if (!center.Has(HospitalFeature.EditPatient)) return FeatureDisabled();

        var request = await _service.GetForCenterDetailAsync(id, center.Id);
        if (request is null) return NotFound();

        ViewData["Title"] = "ویرایش بیمار";
        ViewData["PatientId"] = id;
        return View(ToInput(request));
    }

    [HttpPost("patient/{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, SupportRequestInput input)
    {
        var center = await PrepareAsync();
        if (center is null) return View("NoCenter");
        if (!center.Has(HospitalFeature.EditPatient)) return FeatureDisabled();
        ViewData["Title"] = "ویرایش بیمار";
        ViewData["PatientId"] = id;

        // رضایت در ویرایش لازم نیست؛ خطای احتمالی آن را نادیده می‌گیریم.
        ModelState.Remove(nameof(SupportRequestInput.DataProcessingConsent));
        if (!ModelState.IsValid) return View(input);

        var ok = await _service.UpdateForCenterAsync(id, center.Id, input);
        if (!ok) return NotFound();
        TempData["Success"] = "اطلاعات بیمار به‌روزرسانی شد.";
        return RedirectToAction(nameof(Patient), new { id });
    }

    [HttpPost("patient/{id}/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(long id, string? title, IFormFile? file)
    {
        var center = await PrepareAsync();
        if (center is null) return View("NoCenter");
        if (!center.Has(HospitalFeature.DocumentUpload)) return FeatureDisabled();

        var request = await _service.GetForCenterDetailAsync(id, center.Id);
        if (request is null) return NotFound();

        var up = await _uploads.SaveAsync(file);
        if (!up.Success) { TempData["Error"] = up.Error; return RedirectToAction(nameof(Patient), new { id }); }

        var docTitle = string.IsNullOrWhiteSpace(title) ? up.Media!.FileName : title.Trim();
        await _service.AddDocumentAsync(id, up.Media!.Id, docTitle, uploadedByApplicant: false);
        TempData["Success"] = "مدرک بارگذاری شد.";
        return RedirectToAction(nameof(Patient), new { id });
    }

    [HttpPost("patient/{id}/message")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Message(long id, string? body)
    {
        var center = await PrepareAsync();
        if (center is null) return View("NoCenter");
        if (!center.Has(HospitalFeature.MessageExperts)) return FeatureDisabled();

        var ok = await _service.AddCenterMessageAsync(id, center.Id, center.Name, body ?? "");
        TempData[ok ? "Success" : "Error"] = ok ? "پیام شما برای کارشناسان ثبت شد." : "ارسال پیام ناموفق بود.";
        return RedirectToAction(nameof(Patient), new { id });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var center = await GetCenterAsync();
        if (center is null) return View("NoCenter");
        if (!center.Has(HospitalFeature.ExportPatients)) return FeatureDisabled();

        var rows = await _service.GetAllForCenterAsync(center.Id);
        var sb = new StringBuilder();
        sb.AppendLine("کد پیگیری,نام بیمار,موبایل,نوع نیاز,وضعیت,تاریخ ثبت");
        foreach (var r in rows)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";
            sb.Append(C(r.TrackingCode)).Append(',')
              .Append(C(r.ApplicantName)).Append(',')
              .Append(C(r.Mobile)).Append(',')
              .Append(C(r.NeedType)).Append(',')
              .Append(C(SupportRequestStatusFa(r.Status))).Append(',')
              .Append(C(r.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd")))
              .Append('\n');
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"patients-{center.Slug}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private IActionResult FeatureDisabled()
    {
        ViewData["Title"] = "غیرفعال";
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("FeatureDisabled");
    }

    private static SupportRequestInput ToInput(SupportRequest r) => new()
    {
        ApplicantName = r.ApplicantName,
        Mobile = r.Mobile,
        NationalId = r.NationalId,
        Province = r.Province,
        City = r.City,
        Village = r.Village,
        ReferredBy = r.ReferredBy,
        DialysisType = r.DialysisType,
        SessionsPerWeek = r.SessionsPerWeek,
        NeedType = r.NeedType,
        EstimatedCost = r.EstimatedCost,
        Description = r.Description,
        DataProcessingConsent = true,
    };

    private static string SupportRequestStatusFa(SupportRequestStatus s) => s switch
    {
        SupportRequestStatus.Submitted => "ثبت اولیه",
        SupportRequestStatus.PendingReview => "در انتظار بررسی",
        SupportRequestStatus.NeedsDocuments => "نیازمند مدارک",
        SupportRequestStatus.SocialWorkerReview => "بررسی مددکار",
        SupportRequestStatus.MedicalReview => "بررسی پزشکی",
        SupportRequestStatus.PreliminaryApproved => "تأیید اولیه",
        SupportRequestStatus.Rejected => "ردشده",
        SupportRequestStatus.FinalApproved => "تأیید نهایی",
        SupportRequestStatus.SupportAssigned => "تخصیص حمایت",
        SupportRequestStatus.InProgress => "در حال اجرا",
        SupportRequestStatus.Completed => "تکمیل‌شده",
        SupportRequestStatus.Archived => "بایگانی",
        _ => s.ToString(),
    };
}
