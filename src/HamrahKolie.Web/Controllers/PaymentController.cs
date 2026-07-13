using HamrahKolie.Application.Donations;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Controllers;

/// <summary>
/// مسیرهای درگاه پرداخت: صفحه شبیه‌سازی درگاه آزمایشی و بازگشت (Callback) با تأیید سمت سرور.
/// </summary>
public class PaymentController : Controller
{
    private readonly IDonationService _donations;
    private readonly ApplicationDbContext _db;

    public PaymentController(IDonationService donations, ApplicationDbContext db)
    {
        _donations = donations;
        _db = db;
    }

    /// <summary>صفحه شبیه‌سازی درگاه آزمایشی (فقط برای Development/Provider آزمایشی).</summary>
    [HttpGet("/payment/simulate")]
    public async Task<IActionResult> Simulate([FromQuery] string authority)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Include(p => p.Donation)
            .FirstOrDefaultAsync(p => p.Authority == authority);
        if (payment is null) return NotFound();

        ViewData["Title"] = "درگاه پرداخت آزمایشی";
        ViewData["Robots"] = "noindex,nofollow";
        ViewData["Authority"] = authority;
        ViewData["Amount"] = payment.Amount;
        ViewData["Tracking"] = payment.Donation.TrackingCode;
        return View();
    }

    /// <summary>بازگشت از درگاه؛ تأیید سمت سرور و هدایت به رسید.</summary>
    [HttpGet("/payment/callback")]
    public async Task<IActionResult> Callback([FromQuery] string authority, [FromQuery] string? status)
    {
        var callbackParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in Request.Query)
            callbackParams[kv.Key] = kv.Value.ToString();

        var result = await _donations.HandleCallbackAsync(authority, callbackParams);

        if (result.Success)
            return RedirectToAction("Success", "Donate", new { code = result.TrackingCode });

        return RedirectToAction("Failed", "Donate", new { code = result.TrackingCode });
    }
}
