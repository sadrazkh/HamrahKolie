using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Web.Areas.Admin.ViewModels;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.SettingsManage)]
public class SettingsController : Controller
{
    private readonly ISettingService _settings;
    private readonly IAuditService _audit;

    public SettingsController(ISettingService settings, IAuditService audit)
    {
        _settings = settings;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "تنظیمات مؤسسه";
        var vm = new SettingsViewModel
        {
            OrganizationName = await _settings.GetAsync("organization.name"),
            Slogan = await _settings.GetAsync("organization.slogan"),
            Message = await _settings.GetAsync("organization.message"),
            Email = await _settings.GetAsync("organization.email"),
            Phone = await _settings.GetAsync("organization.phone"),
            Address = await _settings.GetAsync("organization.address"),
            SeoTitle = await _settings.GetAsync("seo.default_title"),
            SeoDescription = await _settings.GetAsync("seo.default_description"),
            OfflineAccount = await _settings.GetAsync("payment.offline_account"),
            MaintenanceMode = string.Equals(await _settings.GetAsync("site.maintenance_mode"), "true", StringComparison.OrdinalIgnoreCase),
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel vm)
    {
        ViewData["Title"] = "تنظیمات مؤسسه";
        await _settings.SetAsync("organization.name", vm.OrganizationName);
        await _settings.SetAsync("organization.slogan", vm.Slogan);
        await _settings.SetAsync("organization.message", vm.Message);
        await _settings.SetAsync("organization.email", vm.Email);
        await _settings.SetAsync("organization.phone", vm.Phone);
        await _settings.SetAsync("organization.address", vm.Address);
        await _settings.SetAsync("seo.default_title", vm.SeoTitle);
        await _settings.SetAsync("seo.default_description", vm.SeoDescription);
        await _settings.SetAsync("payment.offline_account", vm.OfflineAccount);
        await _settings.SetAsync("site.maintenance_mode", vm.MaintenanceMode ? "true" : "false");

        await _audit.LogAsync("Settings.Update", "تنظیمات مؤسسه به‌روزرسانی شد.");
        TempData["Success"] = "تنظیمات ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}
