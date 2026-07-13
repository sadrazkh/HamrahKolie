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
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DashboardController(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _db = db;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "داشبورد";

        if (_environment.IsDevelopment()
            && _configuration.GetValue<bool>("PresentationMode:Enabled")
            && User.HasClaim("presentation_mode", "true"))
        {
            return View(new DashboardViewModel
            {
                UsersCount = 24,
                RolesCount = Roles.All.Count,
                PermissionsCount = Permissions.All.Count,
                SettingsCount = 8,
                AuditLogsToday = 12,
                RecentAudits = new[]
                {
                    new RecentAuditItem(DateTime.UtcNow.AddMinutes(-8), "Content.Published", "مدیر نسخه نمایشی", "انتشار مطلب نمونه"),
                    new RecentAuditItem(DateTime.UtcNow.AddMinutes(-21), "Media.Uploaded", "مدیر محتوا", "بارگذاری تصویر کمپین"),
                    new RecentAuditItem(DateTime.UtcNow.AddHours(-1), "User.RoleChanged", "مدیر نسخه نمایشی", "تغییر نقش کاربر نمونه"),
                },
            });
        }

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
