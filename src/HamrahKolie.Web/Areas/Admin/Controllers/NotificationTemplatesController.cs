using HamrahKolie.Application.Authorization;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.SettingsManage)]
public class NotificationTemplatesController : Controller
{
    private readonly ApplicationDbContext _db;
    public NotificationTemplatesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "قالب‌های پیام";
        var items = await _db.NotificationTemplates.AsNoTracking()
            .OrderBy(t => t.Key).ThenBy(t => t.Channel).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var t = await _db.NotificationTemplates.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        ViewData["Title"] = "ویرایش قالب";
        return View(t);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, string? subject, string body, bool isEnabled)
    {
        var t = await _db.NotificationTemplates.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        t.Subject = subject?.Trim();
        t.Body = body?.Trim() ?? t.Body;
        t.IsEnabled = isEnabled;
        await _db.SaveChangesAsync();
        TempData["Success"] = "قالب ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}
