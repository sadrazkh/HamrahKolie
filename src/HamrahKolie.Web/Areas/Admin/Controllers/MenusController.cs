using HamrahKolie.Application.Authorization;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.MenuManage)]
public class MenusController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public MenusController(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "مدیریت منوها";
        var menus = await _db.Menus
            .Include(m => m.Items.OrderBy(i => i.SortOrder))
            .AsNoTracking()
            .OrderBy(m => m.Location)
            .ToListAsync();
        return View(menus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(long menuId, string title, string url, int sortOrder, bool openInNewTab)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
        {
            TempData["Error"] = "عنوان و نشانی آیتم را وارد کنید.";
            return RedirectToAction(nameof(Index));
        }
        if (!await _db.Menus.AnyAsync(m => m.Id == menuId)) return NotFound();

        _db.MenuItems.Add(new MenuItem
        {
            MenuId = menuId, Title = title.Trim(), Url = url.Trim(),
            SortOrder = sortOrder, OpenInNewTab = openInNewTab
        });
        await _db.SaveChangesAsync();
        InvalidateNavCache();
        TempData["Success"] = "آیتم منو افزوده شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(long id)
    {
        var item = await _db.MenuItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound();
        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
        InvalidateNavCache();
        TempData["Success"] = "آیتم منو حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    private void InvalidateNavCache()
    {
        _cache.Remove("nav:Header");
        _cache.Remove("nav:Footer");
    }
}
