using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.SupportRequests;
using HamrahKolie.Web.Services;
using HamrahKolie.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HamrahKolie.Web.Controllers;

/// <summary>ثبت و پیگیری درخواست حمایت (بدون نیاز به حساب کاربری؛ پیگیری با OTP).</summary>
[Route("support")]
public class SupportController : Controller
{
    private const string ConsentVersion = "v1";
    private const string OtpPurpose = "support_track";

    private readonly ISupportRequestService _service;
    private readonly IOtpService _otp;
    private readonly IWebHostEnvironment _env;
    private readonly IFileUploadService _uploads;

    public SupportController(ISupportRequestService service, IOtpService otp, IWebHostEnvironment env, IFileUploadService uploads)
    {
        _service = service;
        _otp = otp;
        _env = env;
        _uploads = uploads;
    }

    // ── ثبت درخواست ──────────────────────────────────────────────
    [HttpGet("request")]
    public IActionResult Request()
    {
        ViewData["Title"] = "درخواست حمایت";
        return View(new SupportRequestInput());
    }

    [HttpPost("request")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Request(SupportRequestInput input, List<IFormFile>? documents)
    {
        ViewData["Title"] = "درخواست حمایت";
        if (!ModelState.IsValid) return View(input);

        var (id, tracking) = await _service.SubmitAsync(input, ConsentVersion);

        // آپلود مدارک همراه ثبت اولیه (اختیاری).
        if (documents is not null)
        {
            foreach (var file in documents.Where(f => f.Length > 0).Take(10))
            {
                var up = await _uploads.SaveAsync(file);
                if (up.Success)
                    await _service.AddDocumentAsync(id, up.Media!.Id, up.Media.FileName, uploadedByApplicant: true);
            }
        }

        TempData["Tracking"] = tracking;
        return RedirectToAction(nameof(Submitted));
    }

    [HttpGet("submitted")]
    public IActionResult Submitted()
    {
        ViewData["Title"] = "درخواست ثبت شد";
        ViewData["Robots"] = "noindex,nofollow";
        var tracking = TempData["Tracking"] as string;
        if (string.IsNullOrEmpty(tracking)) return RedirectToAction(nameof(Request));
        return View(model: tracking);
    }

    // ── پیگیری با OTP ────────────────────────────────────────────
    [HttpGet("track")]
    public IActionResult Track()
    {
        ViewData["Title"] = "پیگیری درخواست";
        return View(new SupportTrackViewModel());
    }

    [HttpPost("track")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> SendOtp(SupportTrackViewModel vm)
    {
        ViewData["Title"] = "پیگیری درخواست";
        var request = await _service.GetForApplicantAsync(vm.TrackingCode, vm.Mobile);
        if (request is null)
        {
            vm.Error = "درخواستی با این کد پیگیری و موبایل یافت نشد.";
            return View("Track", vm);
        }

        var key = $"{vm.TrackingCode.Trim().ToUpperInvariant()}|{vm.Mobile.Trim()}";
        var result = await _otp.RequestAsync(OtpPurpose, key, vm.Mobile.Trim());

        vm.Step = "otp";
        vm.DevCode = _env.IsDevelopment() ? result.DevCode : null;
        return View("Track", vm);
    }

    [HttpPost("track/verify")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> VerifyOtp(SupportTrackViewModel vm, string code)
    {
        ViewData["Title"] = "پیگیری درخواست";
        var normalizedCode = vm.TrackingCode.Trim().ToUpperInvariant();
        var key = $"{normalizedCode}|{vm.Mobile.Trim()}";

        if (!await _otp.VerifyAsync(OtpPurpose, key, code))
        {
            vm.Step = "otp";
            vm.Error = "کد واردشده نادرست یا منقضی شده است.";
            return View("Track", vm);
        }

        // ثبت دسترسی تأییدشده در نشست.
        HttpContext.Session.SetString(SessionKey(normalizedCode), vm.Mobile.Trim());
        return RedirectToAction(nameof(View), new { code = normalizedCode });
    }

    [HttpGet("view")]
    public async Task<IActionResult> View(string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var mobile = HttpContext.Session.GetString(SessionKey(normalizedCode));
        if (string.IsNullOrEmpty(mobile)) return RedirectToAction(nameof(Track));

        var request = await _service.GetForApplicantAsync(normalizedCode, mobile);
        if (request is null) return RedirectToAction(nameof(Track));

        ViewData["Title"] = "وضعیت درخواست";
        ViewData["Robots"] = "noindex,nofollow";
        return View(new SupportViewPageModel { Request = request });
    }

    [HttpPost("reply")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Reply(string code, long requestId, string body)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var mobile = HttpContext.Session.GetString(SessionKey(normalizedCode));
        if (string.IsNullOrEmpty(mobile)) return RedirectToAction(nameof(Track));

        await _service.AddApplicantMessageAsync(requestId, mobile, body);
        TempData["Success"] = "پیام شما ثبت شد.";
        return RedirectToAction(nameof(View), new { code = normalizedCode });
    }

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Upload(string code, long requestId, string? title, IFormFile? file)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var mobile = HttpContext.Session.GetString(SessionKey(normalizedCode));
        if (string.IsNullOrEmpty(mobile)) return RedirectToAction(nameof(Track));

        var up = await _uploads.SaveAsync(file);
        if (!up.Success)
        {
            TempData["Error"] = up.Error;
            return RedirectToAction(nameof(View), new { code = normalizedCode });
        }

        var docTitle = string.IsNullOrWhiteSpace(title) ? up.Media!.FileName : title.Trim();
        await _service.AddApplicantDocumentAsync(requestId, mobile, up.Media!.Id, docTitle);
        TempData["Success"] = "مدرک شما بارگذاری شد.";
        return RedirectToAction(nameof(View), new { code = normalizedCode });
    }

    private static string SessionKey(string code) => $"sr:verified:{code}";
}
