using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Donations;
using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Controllers;

[Route("donate")]
public class DonateController : Controller
{
    private readonly IDonationService _donations;
    private readonly ApplicationDbContext _db;
    private readonly ISettingService _settings;
    private readonly IPageBuilderService _pageBuilder;
    private readonly HamrahKolie.Web.Services.IFileUploadService _uploads;

    public DonateController(IDonationService donations, ApplicationDbContext db, ISettingService settings,
        IPageBuilderService pageBuilder, HamrahKolie.Web.Services.IFileUploadService uploads)
    {
        _donations = donations;
        _db = db;
        _settings = settings;
        _pageBuilder = pageBuilder;
        _uploads = uploads;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? campaign)
    {
        ViewData["Title"] = "حمایت مالی";
        await LoadPageSectionsAsync();
        var vm = new DonateViewModel { Campaigns = await ActiveCampaignsAsync() };

        if (!string.IsNullOrWhiteSpace(campaign))
        {
            var c = await _db.Campaigns.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == campaign && x.Status == CampaignStatus.Active);
            if (c is not null)
            {
                vm.Input.CampaignId = c.Id;
                vm.Input.Type = DonationType.Campaign;
                vm.SelectedCampaignTitle = c.Title;
            }
        }
        return View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Index(DonationInput input)
    {
        ViewData["Title"] = "حمایت مالی";
        if (!ModelState.IsValid)
        {
            await LoadPageSectionsAsync();
            return View(new DonateViewModel { Input = input, Campaigns = await ActiveCampaignsAsync() });
        }

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/payment/callback";
        var result = await _donations.CreateOnlineAsync(input, callbackUrl);

        if (!result.Success || result.RedirectUrl is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ایجاد پرداخت.");
            await LoadPageSectionsAsync();
            return View(new DonateViewModel { Input = input, Campaigns = await ActiveCampaignsAsync() });
        }

        return Redirect(result.RedirectUrl);
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success([FromQuery] string code)
    {
        var donation = await _donations.GetByTrackingCodeAsync(code);
        if (donation is null) return NotFound();
        ViewData["Title"] = "رسید حمایت";
        ViewData["Robots"] = "noindex,nofollow";
        return View(donation);
    }

    [HttpGet("failed")]
    public async Task<IActionResult> Failed([FromQuery] string? code)
    {
        ViewData["Title"] = "پرداخت ناموفق";
        ViewData["Robots"] = "noindex,nofollow";
        var donation = string.IsNullOrEmpty(code) ? null : await _donations.GetByTrackingCodeAsync(code);
        return View(donation);
    }

    [HttpGet("track")]
    public IActionResult Track()
    {
        ViewData["Title"] = "پیگیری کمک";
        return View(new TrackDonationViewModel());
    }

    [HttpPost("track")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Track(TrackDonationViewModel vm)
    {
        ViewData["Title"] = "پیگیری کمک";
        vm.Searched = true;
        if (!string.IsNullOrWhiteSpace(vm.TrackingCode) && !string.IsNullOrWhiteSpace(vm.Mobile))
            vm.Result = await _donations.GetByTrackingAsync(vm.TrackingCode, vm.Mobile);
        return View(vm);
    }

    [HttpGet("offline")]
    public async Task<IActionResult> Offline([FromQuery] string? campaign)
    {
        ViewData["Title"] = "پرداخت آفلاین (ثبت فیش)";
        var vm = new OfflineDonateViewModel
        {
            Campaigns = await ActiveCampaignsAsync(),
            BankAccountInfo = await _settings.GetAsync("payment.offline_account"),
        };
        if (!string.IsNullOrWhiteSpace(campaign))
        {
            var c = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == campaign);
            if (c is not null) vm.Input.CampaignId = c.Id;
        }
        return View(vm);
    }

    [HttpPost("offline")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Offline(OfflineDonationInput input, IFormFile? receipt)
    {
        ViewData["Title"] = "پرداخت آفلاین (ثبت فیش)";
        if (!ModelState.IsValid)
        {
            return View(new OfflineDonateViewModel
            {
                Input = input,
                Campaigns = await ActiveCampaignsAsync(),
                BankAccountInfo = await _settings.GetAsync("payment.offline_account"),
            });
        }

        // آپلود تصویر فیش (اختیاری).
        if (receipt is { Length: > 0 })
        {
            var up = await _uploads.SaveAsync(receipt);
            if (up.Success) input.ReceiptImageId = up.Media!.Id;
        }

        var tracking = await _donations.SubmitOfflineAsync(input);
        TempData["Success"] = "فیش شما ثبت شد و پس از بررسی توسط کارشناسان تأیید می‌شود.";
        return RedirectToAction(nameof(Track));
    }

    private async Task<IReadOnlyList<SelectListItem>> ActiveCampaignsAsync()
        => await _db.Campaigns.AsNoTracking()
            .Where(c => c.Status == CampaignStatus.Active)
            .OrderBy(c => c.Title)
            .Select(c => new SelectListItem(c.Title, c.Id.ToString()))
            .ToListAsync();

    private async Task LoadPageSectionsAsync()
    {
        try { ViewData["PageBuilderSections"] = await _pageBuilder.GetVisibleAsync("donate"); }
        catch { }
    }
}
