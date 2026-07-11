using HamrahKolie.Application.Authorization;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.UserView)]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "کاربران";

        // نگاشت نقش‌های هر کاربر با یک پرس‌وجوی گروهی برای جلوگیری از N+1.
        var userRoles = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.DisplayName ?? r.Name })
            .ToListAsync();

        var rolesByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => string.Join("، ", g.Select(x => x.RoleName)));

        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id, u.Email, u.FirstName, u.LastName, u.IsActive, u.CreatedAt, u.LastLoginAt
            })
            .ToListAsync();

        var list = users.Select(u => new UserListItem(
            u.Id,
            u.Email,
            string.Join(" ", new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
            u.IsActive,
            u.CreatedAt,
            u.LastLoginAt,
            rolesByUser.TryGetValue(u.Id, out var r) ? r : "—")).ToList();

        return View(list);
    }
}
