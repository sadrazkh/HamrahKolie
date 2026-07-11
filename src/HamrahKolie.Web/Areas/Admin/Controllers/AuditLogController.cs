using HamrahKolie.Application.Authorization;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.AuditLogView)]
public class AuditLogController : Controller
{
    private const int PageSize = 30;
    private readonly ApplicationDbContext _db;

    public AuditLogController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData["Title"] = "گزارش رویدادها";
        page = Math.Max(1, page);

        var total = await _db.AuditLogs.CountAsync();
        var items = await _db.AuditLogs
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .AsNoTracking()
            .ToListAsync();

        ViewData["Page"] = page;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)PageSize);
        return View(items);
    }
}
