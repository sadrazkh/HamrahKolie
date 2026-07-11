using HamrahKolie.Application.Authorization;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.RoleManage)]
public class RolesController : Controller
{
    private readonly ApplicationDbContext _db;

    public RolesController(ApplicationDbContext db) => _db = db;

    /// <summary>فهرست نقش‌ها به همراه تعداد دسترسی هر نقش.</summary>
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "نقش‌ها و دسترسی‌ها";

        var roles = await _db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItem(
                r.Id,
                r.Name!,
                r.DisplayName,
                r.Description,
                r.IsSystemRole,
                r.Name == Roles.SuperAdmin
                    ? Permissions.All.Count
                    : _db.RolePermissions.Count(rp => rp.RoleId == r.Id)))
            .ToListAsync();

        return View(roles);
    }
}
