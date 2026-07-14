using System.Security.Claims;
using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Identity;
using HamrahKolie.Infrastructure.Identity;
using HamrahKolie.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Controllers;

/// <summary>ورود و خروج کاربران (پنل مدیریت و کاربران عمومی).</summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService audit,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet("/presentation/admin")]
    [AllowAnonymous]
    public async Task<IActionResult> PresentationAdmin(string? returnUrl = null)
    {
        if (!_environment.IsDevelopment()
            || !_configuration.GetValue<bool>("PresentationMode:Enabled"))
        {
            return NotFound();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "presentation-admin"),
            new Claim(ClaimTypes.Name, "مدیر نسخه نمایشی"),
            new Claim(ClaimTypes.Email, "presentation@localhost"),
            new Claim(ClaimTypes.Role, Roles.SuperAdmin),
            new Claim(AppClaimTypes.Permission, "*"),
            new Claim("presentation_mode", "true"),
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme));

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4),
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["Title"] = "ورود";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["Title"] = "ورود";
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور نادرست است.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _audit.LogAsync("Login.Success", $"ورود موفق کاربر {user.Email}");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            // کاربران مرکز درمانی به پورتال خودشان هدایت می‌شوند (نه پنل مدیریت).
            if (await _userManager.IsInRoleAsync(user, Roles.MedicalCenter))
            {
                return RedirectToAction("Index", "Hospital");
            }

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "به دلیل تلاش‌های ناموفق، حساب موقتاً قفل شده است. کمی بعد دوباره تلاش کنید.");
        }
        else
        {
            await _audit.LogAsync("Login.Failed", $"ورود ناموفق برای {model.Email}");
            ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور نادرست است.");
        }
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "دسترسی غیرمجاز";
        return View();
    }
}
