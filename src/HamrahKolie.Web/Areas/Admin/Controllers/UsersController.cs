using System.ComponentModel.DataAnnotations;
using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Identity;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.UserView)]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "کاربران";

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
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.IsActive, u.CreatedAt, u.LastLoginAt })
            .ToListAsync();

        var list = users.Select(u => new UserListItem(
            u.Id, u.Email,
            string.Join(" ", new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
            u.IsActive, u.CreatedAt, u.LastLoginAt,
            rolesByUser.TryGetValue(u.Id, out var r) ? r : "—")).ToList();

        return View(list);
    }

    // ── ساخت حساب مرکز درمانی ────────────────────────────────────
    [HttpGet]
    [HasPermission(Permissions.UserManage)]
    public async Task<IActionResult> CreateHospital()
    {
        ViewData["Title"] = "ساخت حساب مرکز درمانی";
        await PopulateCentersAsync();
        return View(new CreateHospitalInput());
    }

    [HttpPost]
    [HasPermission(Permissions.UserManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHospital(CreateHospitalInput input)
    {
        ViewData["Title"] = "ساخت حساب مرکز درمانی";
        if (!ModelState.IsValid) { await PopulateCentersAsync(); return View(input); }

        if (await _userManager.FindByEmailAsync(input.Email) is not null)
        {
            ModelState.AddModelError(nameof(input.Email), "کاربری با این ایمیل وجود دارد.");
            await PopulateCentersAsync();
            return View(input);
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            EmailConfirmed = true,
            FirstName = input.DisplayName,
            IsActive = true,
            CenterId = input.CenterId,
        };
        var result = await _userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            await PopulateCentersAsync();
            return View(input);
        }

        await _userManager.AddToRoleAsync(user, Roles.MedicalCenter);
        await _audit.LogAsync("User.CreateHospital", $"حساب مرکز درمانی برای {input.Email} ساخته شد.", "User", user.Id);
        TempData["Success"] = "حساب مرکز درمانی ساخته شد.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCentersAsync()
    {
        ViewBag.Centers = await _db.DialysisCenters.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name + (c.City != null ? $" — {c.City}" : ""), c.Id.ToString()))
            .ToListAsync();
    }
}

public class CreateHospitalInput
{
    [Required(ErrorMessage = "ایمیل را وارد کنید.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "نام نمایشی")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "رمز عبور باید حداقل ۸ نویسه باشد.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "مرکز درمانی را انتخاب کنید.")]
    public long? CenterId { get; set; }
}
