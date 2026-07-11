using HamrahKolie.Application.Authorization;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.DashboardView)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "داشبورد";
        var today = DateTime.UtcNow.Date;

        var vm = new DashboardViewModel
        {
            UsersCount = await _db.Users.CountAsync(),
            RolesCount = await _db.Roles.CountAsync(),
            PermissionsCount = await _db.Permissions.CountAsync(),
            SettingsCount = await _db.Settings.CountAsync(),
            AuditLogsToday = await _db.AuditLogs.CountAsync(a => a.OccurredAt >= today),
            RecentAudits = await _db.AuditLogs
                .OrderByDescending(a => a.OccurredAt)
                .Take(8)
                .Select(a => new RecentAuditItem(a.OccurredAt, a.Action, a.UserName, a.Description))
                .ToListAsync(),
        };

        return View(vm);
    }
}
