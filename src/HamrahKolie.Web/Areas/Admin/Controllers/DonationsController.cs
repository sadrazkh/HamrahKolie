using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Donations;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.DonationView)]
public class DonationsController : Controller
{
    private const int PageSize = 25;
    private readonly IDonationService _donations;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public DonationsController(IDonationService donations, ICurrentUser currentUser, IAuditService audit)
    {
        _donations = donations;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index(PaymentStatus? status, PaymentMethod? method, int page = 1)
    {
        ViewData["Title"] = "کمک‌ها";
        ViewData["Status"] = status;
        ViewData["Method"] = method;
        ViewData["PendingOffline"] = await _donations.GetPendingOfflineCountAsync();
        var result = await _donations.GetAdminListAsync(status, method, null, page, PageSize);
        return View(result);
    }

    public async Task<IActionResult> Detail(long id)
    {
        var donation = await _donations.GetAdminDetailAsync(id);
        if (donation is null) return NotFound();
        ViewData["Title"] = $"کمک {donation.TrackingCode}";
        return View(donation);
    }

    [HttpPost]
    [HasPermission(Permissions.DonationVerifyOffline)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveOffline(long id, string? note)
    {
        var ok = await _donations.ApproveOfflineAsync(id, _currentUser.UserName, note);
        if (!ok) { TempData["Error"] = "امکان تأیید این پرداخت نیست."; return RedirectToAction(nameof(Detail), new { id }); }
        await _audit.LogAsync("Donation.ApproveOffline", "پرداخت آفلاین تأیید شد.", "Donation", id.ToString());
        TempData["Success"] = "پرداخت آفلاین تأیید و به کمک رسمی تبدیل شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.DonationVerifyOffline)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOffline(long id, string? note)
    {
        var ok = await _donations.RejectOfflineAsync(id, _currentUser.UserName, note);
        if (!ok) { TempData["Error"] = "امکان رد این پرداخت نیست."; return RedirectToAction(nameof(Detail), new { id }); }
        await _audit.LogAsync("Donation.RejectOffline", "پرداخت آفلاین رد شد.", "Donation", id.ToString());
        TempData["Success"] = "پرداخت آفلاین رد شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [HasPermission(Permissions.DonationRefund)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(long id)
    {
        var ok = await _donations.RefundAsync(id, _currentUser.UserName);
        if (!ok) { TempData["Error"] = "امکان بازپرداخت این کمک نیست."; return RedirectToAction(nameof(Detail), new { id }); }
        await _audit.LogAsync("Donation.Refund", "کمک بازپرداخت شد.", "Donation", id.ToString());
        TempData["Success"] = "بازپرداخت ثبت شد.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
